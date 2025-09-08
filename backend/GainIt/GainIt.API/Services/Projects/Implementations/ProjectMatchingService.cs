using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using GainIt.API.Data;
using GainIt.API.DTOs.Search;
using GainIt.API.DTOs.ViewModels.Projects;
using GainIt.API.Models.Enums.Projects;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Options;
using GainIt.API.Services.Projects.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Text.Json;
using System.ClientModel;


namespace GainIt.API.Services.Projects.Implementations
{
    public class ProjectMatchingService : IProjectMatchingService
    {
        private readonly SearchClient r_searchClient;
        private readonly AzureOpenAIClient r_azureOpenAIClient;
        private readonly string r_embeddingModelName;
        private readonly string r_chatModelName;
        private readonly ChatClient r_chatClient;
        private readonly GainItDbContext r_DbContext;
        private readonly ILogger<ProjectMatchingService> r_logger;
        private const double r_similarityThreshold = 0.75;

        public ProjectMatchingService(SearchClient i_SearchClient, AzureOpenAIClient i_AzureOpenAIClient, IOptions<OpenAIOptions> i_OpenAIOptionsAccessor, GainItDbContext i_DbContext, ILogger<ProjectMatchingService> i_logger)
        {
            r_searchClient = i_SearchClient;
            r_azureOpenAIClient = i_AzureOpenAIClient;
            r_embeddingModelName = i_OpenAIOptionsAccessor.Value.EmbeddingDeploymentName;
            r_chatClient = i_AzureOpenAIClient.GetChatClient(i_OpenAIOptionsAccessor.Value.ChatDeploymentName);
            r_chatModelName = i_OpenAIOptionsAccessor.Value.ChatDeploymentName;
            r_DbContext = i_DbContext;
            r_logger = i_logger;
        }

        public async Task<EnhancedProjectMatchResultDto> MatchProjectsByTextAsync(string i_InputText, int i_ResultCount = 3)
        {
            r_logger.LogInformation("Matching projects by text: InputText={InputText}, ResultCount={ResultCount}", i_InputText, i_ResultCount);

            try
            {
                var chatrefinedQuery = await refineQueryWithChatAsync(i_InputText);
                r_logger.LogInformation("Query refined with chat: OriginalQuery={OriginalQuery}, RefinedQuery={RefinedQuery}", i_InputText, chatrefinedQuery);

                var embedding = await getEmbeddingAsync(chatrefinedQuery);
                r_logger.LogInformation("Embedding generated: EmbeddingSize={EmbeddingSize}", embedding.Count);

                // Use iterative search to ensure we get exactly the requested count
                var filteredProjects = await getFilteredProjectsWithIterativeSearchAsync(embedding, chatrefinedQuery, i_ResultCount);
                r_logger.LogInformation("Projects filtered with iterative search: FilteredCount={FilteredCount}", filteredProjects.Count);

                // Convert to AzureVectorSearchProjectViewModel
                var projectViewModels = filteredProjects.Select(ConvertToEnhancedProjectSearchViewModel);

                var chatExplenation = await getChatExplanationAsync(chatrefinedQuery, filteredProjects);
                r_logger.LogInformation("Chat explanation generated: Chat Explanation={Explanation}", chatExplenation);

                var result = new EnhancedProjectMatchResultDto(projectViewModels, chatExplenation);
                // Replace the existing logging line with colored project names using ANSI escape codes for green
                r_logger.LogInformation(
                    "Project matching completed successfully: FinalResultCount={FinalResultCount}, ProjectNames={ProjectNames}",
                    filteredProjects.Count,
                    string.Join(", ", filteredProjects.Select(p => $"\u001b[32m{p.ProjectName}\u001b[0m"))
                );
                return result;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error matching projects by text: InputText={InputText}, ResultCount={ResultCount}", i_InputText, i_ResultCount);
                throw;
            }
        }

        public async Task<IEnumerable<EnhancedProjectSearchViewModel>> MatchProjectsByProfileAsync(Guid i_UserId, int i_ResultCount = 3)
        {
            r_logger.LogInformation("Matching projects by profile: UserId={UserId}, ResultCount={ResultCount}", i_UserId, i_ResultCount);

            try
            {
                var userProfile = await r_DbContext.Users
                    .FirstOrDefaultAsync(u => u.UserId == i_UserId);

                if (userProfile == null)
                {
                    r_logger.LogWarning("User profile not found: UserId={UserId}", i_UserId);
                    throw new KeyNotFoundException("User profile not found.");
                }

                r_logger.LogInformation("User profile found: UserId={UserId}, UserType={UserType}", i_UserId, userProfile.GetType().Name);

                string searchquery = buildProfileQuery(userProfile);
                r_logger.LogInformation("Profile query built: UserId={UserId}, Query={Query}", i_UserId, searchquery);

                var chatrefinedQuery = await refineQueryWithChatAsync(searchquery);
                r_logger.LogInformation("Profile query refined with chat: UserId={UserId}, RefinedQuery={RefinedQuery}", i_UserId, chatrefinedQuery);

                var embedding = await getEmbeddingAsync(chatrefinedQuery);
                r_logger.LogInformation("Profile embedding generated: UserId={UserId}, EmbeddingSize={EmbeddingSize}", i_UserId, embedding.Count);

                // Use iterative search to ensure we get exactly the requested count
                var filteredProjects = await getFilteredProjectsWithIterativeSearchAsync(embedding, chatrefinedQuery, i_ResultCount);
                r_logger.LogInformation(
                    "Profile projects filtered with iterative search: UserId={UserId}, FilteredCount={FilteredCount}, ProjectNames={ProjectNames}",
                    i_UserId,
                    filteredProjects.Count,
                    string.Join(", ", filteredProjects.Select(p => $"\u001b[32m{p.ProjectName}\u001b[0m"))
                );

                // Convert to AzureVectorSearchProjectViewModel
                var projectViewModels = filteredProjects.Select(ConvertToEnhancedProjectSearchViewModel);
                return projectViewModels;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error matching projects by profile: UserId={UserId}, ResultCount={ResultCount}", i_UserId, i_ResultCount);
                throw;
            }
        }

        private async Task<string> getChatExplanationAsync(string i_Query, List<TemplateProject> i_MatchedProjects)
        {
            r_logger.LogInformation("Generating chat explanation: Query={Query}, ProjectsCount={ProjectsCount}", i_Query, i_MatchedProjects.Count);

            try
            {
                var summaries = string.Join("\n\n", i_MatchedProjects.Select(p =>
                {
                    var summary =
                        $"ProjectId: {p.ProjectId}\n" +
                        $"Name: {p.ProjectName}\n" +
                        $"Description: {p.ProjectDescription}\n" +
                        $"Difficulty: {p.DifficultyLevel}\n" +
                        $"Duration: {p.Duration.TotalDays} days\n" +
                        $"Goals: [{string.Join(", ", p.Goals ?? new List<string>())}]\n" +
                        $"Technologies: [{string.Join(", ", p.Technologies ?? new List<string>())}]\n" +
                        $"RequiredRoles: [{string.Join(", ", p.RequiredRoles ?? new List<string>())}]";

                    if (p is UserProject userProject)
                    {
                        summary += "\n" +
                            $"Source: {userProject.ProjectSource}\n" +
                            $"Status: {userProject.ProjectStatus}";
                    }

                    return summary;
                }));

                var messages = new ChatMessage[]
                {
                    new SystemChatMessage(
                        "You are an assistant that provides detailed explanations for each project that was selected. " +
                        "Follow these rules:\n" +
                        "1. Give **2-3 sentences** per project explaining why it matches the user's query\n" +
                        "2. Use the format: \"ProjectName: [detailed explanation]\"\n" +
                        "3. Keep each explanation between 25-40 words\n" +
                        "4. Focus on the most relevant aspects that connect to the user's query\n" +
                        "5. Be specific about technologies, difficulty level, key features, and learning outcomes\n" +
                        "6. Mention how the project helps achieve the user's goals\n" +
                        "7. Do not repeat the same explanation for multiple projects"
                    ),
                    new UserChatMessage(
                        $"User query: {i_Query}\n\nSelected projects:\n{summaries}\n\n" +
                        "Provide a detailed explanation for each project:")
                };

                var options = new ChatCompletionOptions
                {
                    Temperature = 0f
                };

                ChatCompletion completion =
                    await r_chatClient.CompleteChatAsync(messages, options);

                var explanation = completion.Content[0].Text.Trim();
                r_logger.LogInformation("Chat explanation generated successfully: ExplanationLength={ExplanationLength}", explanation.Length);
                return explanation;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error generating chat explanation: Query={Query}, ProjectsCount={ProjectsCount}", i_Query, i_MatchedProjects.Count);
                throw;
            }
        }

        private async Task<string> refineQueryWithChatAsync(string i_OriginalQuery)
        {
            r_logger.LogInformation("Refining query with chat: OriginalQuery={OriginalQuery}", i_OriginalQuery);

            try
            {
                var messages = new ChatMessage[]
                {
                    new SystemChatMessage(
                        "You are an assistant that refines search queries for project matching. " +
                        "Make the query more specific and relevant for finding projects. " +
                        "Keep it concise (under 50 words)."),
                    new UserChatMessage($"Refine this search query: {i_OriginalQuery}")
                };

                var options = new ChatCompletionOptions
                {
                    Temperature = 0f
                };

                ChatCompletion completion = await r_chatClient.CompleteChatAsync(messages, options);
                var refinedQuery = completion.Content[0].Text.Trim();

                r_logger.LogInformation("Query refined successfully: OriginalQuery={OriginalQuery}, RefinedQuery={RefinedQuery}", i_OriginalQuery, refinedQuery);
                return refinedQuery;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error refining query with chat: OriginalQuery={OriginalQuery}", i_OriginalQuery);
                throw;
            }
        }

        /// <summary>
        /// Generates AI-powered insights for GitHub analytics using the existing GPT configuration
        /// </summary>
        /// <param name="analyticsSummary">Raw GitHub analytics summary</param>
        /// <param name="userQuery">Optional user query for context</param>
        /// <param name="mode">Controls the output style and focus.</param>
        /// <param name="daysPeriod">Number of days that the analytics summary covers.</param>
        /// <returns>Enhanced analytics explanation with AI insights</returns>
        public async Task<string> GetGitHubAnalyticsExplanationAsync(string analyticsSummary, string? userQuery = null, GitHubInsightsMode mode = GitHubInsightsMode.QA, int daysPeriod = 30)
        {
            r_logger.LogInformation("Generating GitHub analytics explanation: SummaryLength={SummaryLength}, HasUserQuery={HasUserQuery}", 
                analyticsSummary.Length, !string.IsNullOrEmpty(userQuery));

            try
            {
                // Truncate overly long summaries to reduce latency and token usage
                var safeSummary = analyticsSummary.Length > 2000 
                    ? analyticsSummary.Substring(0, 2000) + "..." 
                    : analyticsSummary;

                // Light sanitization to avoid triggering content filter and keep questions scoped
                var safeUserQuery = userQuery?.Trim();
                if (!string.IsNullOrWhiteSpace(safeUserQuery))
                {
                    safeUserQuery = safeUserQuery
                        .Replace("timeline", "schedule", StringComparison.OrdinalIgnoreCase)
                        .Replace("finish", "complete", StringComparison.OrdinalIgnoreCase);
                    // Removed "when" replacement to preserve temporal questions
                }

                bool hasQuestion = !string.IsNullOrWhiteSpace(safeUserQuery);
                string systemPrompt;
                string userPrompt;

                switch (mode)
                {
                    case GitHubInsightsMode.UserSummary:
                        systemPrompt = "You are a mentor producing a detailed, motivating progress report for a contributor. " +
                                       "Use only the provided analytics. Output bullet points only. Structure up to 3 sections: " +
                                       "Activity (5–7 bullets: commits, lines +/- , files, active days, issues/PRs breakdowns, reviews, top day/hour, languages, streaks, latest PR/Commit), " +
                                       "Impact (2–3 bullets with concrete outcomes), " +
                                       "Next steps (3–4 specific, project-scoped actions).";
                        userPrompt = $"GitHub Analytics (public data):\nPeriod: last {daysPeriod} days\n\n{safeSummary}\n\nGenerate the three sections as bullets only. Include a first bullet: 'Period: last {daysPeriod} days'.";
                        break;

                    case GitHubInsightsMode.ProjectSummary:
                        systemPrompt = "You are a mentor writing a motivating team status update. Use only the provided analytics. " +
                                       "Output bullets only: start with strongest stat, add 2 concrete wins, include repository health score (and drivers), " +
                                       "then 2–3 next actions grounded in the data, and end with one short, supportive bullet.";
                        userPrompt = $"GitHub Analytics (public data):\nPeriod: last {daysPeriod} days\n\n{safeSummary}\n\nProduce the status update as instructed. Include a first bullet: 'Period: last {daysPeriod} days'.";
                        break;

                    case GitHubInsightsMode.QA:
                    default:
                        if (hasQuestion)
                        {
                            systemPrompt = "You analyze GitHub repository activity and answer the user's specific question using only the provided data. " +
                                           "Focus directly on what the user asked - don't provide general information that doesn't relate to their question. " +
                                           "If the data doesn't contain information to answer their question, say so clearly. " +
                                           "Output 3-5 focused bullets that directly address their question, up to 25 words each.";
                            userPrompt = $"User question: {safeUserQuery}\n\nGitHub Analytics (public data):\nPeriod: last {daysPeriod} days\n\n{safeSummary}\n\nAnswer ONLY the user's specific question. Include one bullet with the analysis period.";
                        }
                        else
                        {
                            systemPrompt = "You analyze GitHub repository activity and produce insights strictly from the provided data. " +
                                           "Respond in four sections with concise bullets: 1) health/activity, 2) team patterns, 3) improvements, 4) successes.";
                            userPrompt = $"GitHub Analytics (public data):\nPeriod: last {daysPeriod} days\n\n{safeSummary}\n\nOutput the four sections as bullets. Start with a bullet stating the analysis period.";
                        }
                        break;
                }

                var messages = new ChatMessage[]
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage(userPrompt)
                };

                var options = new ChatCompletionOptions
                {
                    Temperature = 0.3f
                };

                try
                {
                    ChatCompletion completion = await r_chatClient.CompleteChatAsync(messages, options);
                    var explanation = completion.Content[0].Text.Trim();
                    explanation = NormalizeAnalyticsPeriod(explanation, daysPeriod);
                    r_logger.LogInformation("GitHub analytics explanation generated successfully: ExplanationLength={ExplanationLength}", explanation.Length);
                    return explanation;
                }
                catch (ClientResultException crex) when (crex.Status == 400)
                {
                    // Retry once with a safer, generic prompt (no user query)
                    r_logger.LogWarning(crex, "Content filter triggered. Retrying with generic prompt.");

                    var fallbackMessages = new ChatMessage[]
                    {
                        new SystemChatMessage(systemPrompt),
                        new UserChatMessage($"GitHub Analytics (public data):\nPeriod: last {daysPeriod} days\n\n{safeSummary}\n\nProvide concise insights only from this data. Include the analysis period as a bullet.")
                    };

                    ChatCompletion completion = await r_chatClient.CompleteChatAsync(fallbackMessages, options);
                    var explanation = completion.Content[0].Text.Trim();
                    explanation = NormalizeAnalyticsPeriod(explanation, daysPeriod);
                    r_logger.LogInformation("GitHub analytics explanation generated on retry: ExplanationLength={ExplanationLength}", explanation.Length);
                    return explanation;
                }
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error generating GitHub analytics explanation: SummaryLength={SummaryLength}", analyticsSummary.Length);
                // Return a fallback message instead of throwing to maintain service stability
                return "Unable to generate AI-powered insights at this time. Please try again later.";
            }
        }

        private static string NormalizeAnalyticsPeriod(string text, int daysPeriod)
        {
            if (daysPeriod == 30 || string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            var replacements = new (string fromText, string toText)[]
            {
                ("in the last 30 days", $"in the last {daysPeriod} days"),
                ("over the last 30 days", $"over the last {daysPeriod} days"),
                ("for the last 30 days", $"for the last {daysPeriod} days"),
                ("during the last 30 days", $"during the last {daysPeriod} days"),
                ("past 30 days", $"past {daysPeriod} days"),
                ("last 30 days", $"last {daysPeriod} days"),
                ("in the previous 30 days", $"in the previous {daysPeriod} days")
            };

            foreach (var pair in replacements)
            {
                text = text.Replace(pair.fromText, pair.toText, StringComparison.OrdinalIgnoreCase);
            }

            return text;
        }

        private async Task<List<TemplateProject>> filterProjectsWithChatAsync(string i_Query, List<TemplateProject> i_Projects, int i_RequestedCount)
        {
            r_logger.LogInformation("Filtering projects with chat: Query={Query}, ProjectsCount={ProjectsCount}", i_Query, i_Projects.Count);

            if (i_Projects == null || !i_Projects.Any())
            {
                r_logger.LogInformation("No projects to filter: Query={Query}", i_Query);
                return new List<TemplateProject>();
            }

            try
            {
                var summaries = string.Join("\n\n", i_Projects.Select(p =>
                {
                    var summary =
                        $"ProjectId: {p.ProjectId}\n" +
                        $"Name: {p.ProjectName}\n" +
                        $"Description: {p.ProjectDescription}\n" +
                        $"Difficulty: {p.DifficultyLevel}\n" +
                        $"Duration: {p.Duration.TotalDays} days\n" +
                        $"Goals: [{string.Join(", ", p.Goals ?? new List<string>())}]\n" +
                        $"Technologies: [{string.Join(", ", p.Technologies ?? new List<string>())}]\n" +
                        $"RequiredRoles: [{string.Join(", ", p.RequiredRoles ?? new List<string>())}]";

                    if (p is UserProject userProject)
                    {
                        summary += "\n" +
                            $"Source: {userProject.ProjectSource}\n" +
                            $"Status: {userProject.ProjectStatus}";
                    }

                    return summary;
                }));

                var messages = new ChatMessage[]
                {
                   new SystemChatMessage(
                    "You are an assistant that verifies project relevance based on a user query. " +
                    "Review the projects and return a JSON array of the IDs (as strings) of the most relevant projects. " +
                    "Be INCLUSIVE - include projects that have ANY connection to the query, even if tangential or loosely related. " +
                    "Only exclude projects that are completely unrelated to the query. " +
                    "Consider technologies, skills, domains, difficulty levels, and learning outcomes. " +
                    "If in doubt, include the project. " +
                    "IMPORTANT: Return exactly the number of projects requested. If there are fewer relevant projects than requested, return all relevant ones. " +
                    "If there are more relevant projects than requested, return the most relevant ones up to the requested count. " +
                    "Return the project IDs as a JSON array of strings, e.g., [\"id1\", \"id2\", \"id3\"]."),
                new UserChatMessage(
                    $"Query: {i_Query}\n\nProjects:\n{summaries}\n\nReturn the {i_RequestedCount} most relevant project IDs as a JSON array:")
                };

                var options = new ChatCompletionOptions
                {
                    Temperature = 0f
                };

                ChatCompletion completion =
                    await r_chatClient.CompleteChatAsync(messages, options);
                var response = completion.Content[0].Text.Trim();

                r_logger.LogInformation("Chat filtering response: Query={Query}, Response={Response}", i_Query, response);

                // Remove Markdown code block if present
                if (response.StartsWith("```"))
                {
                    int firstNewline = response.IndexOf('\n');
                    if (firstNewline >= 0)
                    {
                        response = response.Substring(firstNewline + 1);
                    }
                    // Remove trailing ```
                    int lastCodeBlock = response.LastIndexOf("```");
                    if (lastCodeBlock >= 0)
                    {
                        response = response.Substring(0, lastCodeBlock).Trim();
                    }
                }

                Guid[] projectIds;

                if (response == "none" || response == "[]" || string.IsNullOrWhiteSpace(response))
                {
                    r_logger.LogInformation("No projects matched the query: Query={Query}, Response={Response}", i_Query, response);
                    return new List<TemplateProject>();
                }

                try
                {
                    var stringIds = JsonSerializer.Deserialize<string[]>(response);
                    projectIds = stringIds?.Select(Guid.Parse).ToArray() ?? Array.Empty<Guid>();
                    r_logger.LogInformation("Successfully parsed project IDs: Count={Count}, IDs={IDs}", 
                        projectIds.Length, string.Join(",", projectIds));
                }
                catch (Exception parseEx)
                {
                    r_logger.LogWarning(parseEx, "Failed to parse chat response as JSON: Response={Response}", response);
                    // If parsing fails, return all projects instead of none
                    r_logger.LogInformation("Falling back to returning all projects due to parsing error");
                    return i_Projects;
                }

                var filteredProjects = i_Projects
                    .Where(p => projectIds.Contains(p.ProjectId))
                    .ToList();

                r_logger.LogInformation("Projects filtered successfully: Query={Query}, OriginalCount={OriginalCount}, FilteredCount={FilteredCount}",
                    i_Query, i_Projects.Count, filteredProjects.Count);
                return filteredProjects;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error filtering projects with chat: Query={Query}, ProjectsCount={ProjectsCount}", i_Query, i_Projects.Count);
                throw;
            }
        }

        private string buildProfileQuery(User i_userProfile)
        {
            r_logger.LogInformation("Building profile query: UserId={UserId}, UserType={UserType}, FullName={FullName}", i_userProfile.UserId, i_userProfile.GetType().Name, i_userProfile.FullName);

            try
            {
                if (i_userProfile is Gainer gainer)
                {
                    var parts = new List<string>
                    {
                        gainer.Biography ?? string.Empty,
                        gainer.EducationStatus,
                        string.Join(", ", gainer.AreasOfInterest ?? new List<string>())
                    };
                    if (gainer.TechExpertise != null)
                    {
                        parts.Add(gainer.TechExpertise?.ToString() ?? string.Empty);
                    }
                    var query = string.Join(". ", parts);
                    r_logger.LogInformation("Profile query built successfully: UserId={UserId}, QueryLength={QueryLength}", i_userProfile.UserId, query.Length);
                    return query;
                }
                else if (i_userProfile is Mentor mentor)
                {
                    var parts = new List<string>
                    {
                        mentor.Biography ?? string.Empty,
                        mentor.AreaOfExpertise,
                        $"{mentor.YearsOfExperience} years of experience"
                    };
                    if (mentor.TechExpertise != null)
                    {
                        parts.Add(mentor.TechExpertise?.ToString() ?? string.Empty);
                    }
                    var query = string.Join(". ", parts);
                    r_logger.LogInformation("Profile query built successfully: UserId={UserId}, QueryLength={QueryLength}", i_userProfile.UserId, query.Length);
                    return query;
                }
                else
                {
                    throw new ArgumentException("Profile type is not supported for project matching.");
                }
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error building profile query: UserId={UserId}", i_userProfile.UserId);
                throw;
            }
        }

        private async Task<IReadOnlyList<float>> getEmbeddingAsync(string inputText)
        {
            r_logger.LogInformation("Generating embedding: InputTextLength={InputTextLength}", inputText.Length);
            var startTime = DateTime.UtcNow;

            try
            {
                var embeddingClient = r_azureOpenAIClient.GetEmbeddingClient(r_embeddingModelName);
                var embeddingResult = await embeddingClient.GenerateEmbeddingAsync(inputText);
                var embedding = embeddingResult.Value.ToFloats().ToArray();
                var duration = DateTime.UtcNow - startTime;

                r_logger.LogInformation("Embedding generated successfully: EmbeddingSize={EmbeddingSize}, Duration={Duration}ms", embedding.Length, duration.TotalMilliseconds);
                return embedding;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                r_logger.LogError(ex, "Error generating embedding: InputTextLength={InputTextLength}, Duration={Duration}ms", inputText.Length, duration.TotalMilliseconds);
                throw;
            }
        }

        private async Task<List<(Guid ProjectId, double Score)>> runVectorSearchWithScoresAsync(IReadOnlyList<float> embedding, int resultCount, double similarityThreshold)
        {
            r_logger.LogInformation("Running vector search with scores: EmbeddingSize={EmbeddingSize}, ResultCount={ResultCount}, SimilarityThreshold={SimilarityThreshold}", 
                embedding.Count, resultCount, similarityThreshold);
            var startTime = DateTime.UtcNow;

            try
            {
                var searchOptions = new SearchOptions
                {
                    Size = resultCount,
                    VectorSearch = new VectorSearchOptions
                    {
                        Queries = { new VectorizedQuery(embedding.ToArray()) { KNearestNeighborsCount = resultCount, Fields = { "text_vector" } } }
                    }
                };

                var results = await r_searchClient.SearchAsync<ProjectSearchResult>(null, searchOptions);
                
                // Dictionary to store the highest score for each project ID
                var projectScores = new Dictionary<Guid, double>();

                await foreach (var result in results.Value.GetResultsAsync())
                {
                    if (result.Score.HasValue && result.Score.Value >= similarityThreshold)
                    {
                        // Log what we're getting for projectId
                        r_logger.LogInformation("Found search result with score {Score}, ProjectId: '{ProjectId}'", 
                            result.Score.Value, result.Document.projectId ?? "NULL");
                        
                        // Parse the string ProjectId to Guid
                        if (Guid.TryParse(result.Document.projectId, out Guid projectId))
                        {
                            // Keep only the highest score for each project ID
                            if (!projectScores.ContainsKey(projectId) || projectScores[projectId] < result.Score.Value)
                            {
                                projectScores[projectId] = result.Score.Value;
                            }
                        }
                        else
                        {
                            r_logger.LogWarning("Failed to parse ProjectId as Guid: '{ProjectId}'", result.Document.projectId);
                        }
                    }
                }

                // Sort by score (descending) and take the requested number of unique projects
                var matchedProjectIdsWithScores = projectScores
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(resultCount)
                    .Select(kvp => (kvp.Key, kvp.Value))
                    .ToList();

                var duration = DateTime.UtcNow - startTime;
                r_logger.LogInformation("Vector search with scores completed: MatchedProjectIds={MatchedProjectIds}, Count={Count}, Duration={Duration}ms",
                    string.Join(",", matchedProjectIdsWithScores.Select(p => p.Item1)), matchedProjectIdsWithScores.Count, duration.TotalMilliseconds);

                return matchedProjectIdsWithScores;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                r_logger.LogError(ex, "Error in vector search with scores: EmbeddingSize={EmbeddingSize}, ResultCount={ResultCount}, SimilarityThreshold={SimilarityThreshold}, Duration={Duration}ms",
                    embedding.Count, resultCount, similarityThreshold, duration.TotalMilliseconds);
                throw;
            }
        }


        /// <summary>
        /// Performs iterative vector search with chat filtering to ensure we return exactly the requested count of projects.
        /// If chat filtering reduces the results below the requested count, performs additional searches with lower similarity thresholds.
        /// </summary>
        /// <param name="embedding">The embedding vector for search</param>
        /// <param name="query">The refined query for chat filtering</param>
        /// <param name="requestedCount">The exact number of projects to return</param>
        /// <returns>List of filtered projects, exactly the requested count</returns>
        private async Task<List<TemplateProject>> getFilteredProjectsWithIterativeSearchAsync(IReadOnlyList<float> embedding, string query, int requestedCount)
        {
            r_logger.LogInformation("Starting iterative search: RequestedCount={RequestedCount}", requestedCount);
            var startTime = DateTime.UtcNow;

            try
            {
                // Store projects with their scores to maintain ordering
                var projectsWithScores = new List<(TemplateProject Project, double Score)>();
                var allProcessedProjectIds = new HashSet<Guid>(); // Track all processed project IDs to avoid duplicates
                var currentSimilarityThreshold = r_similarityThreshold;
                var searchAttempt = 1;
                const int maxSearchAttempts = 3; // Prevent infinite loops
                const double thresholdDecrement = 0.1; // Reduce threshold by 0.1 each iteration

                while (projectsWithScores.Count < requestedCount && searchAttempt <= maxSearchAttempts)
                {
                    r_logger.LogInformation("Search attempt {Attempt}: CurrentThreshold={Threshold}, CurrentCount={CurrentCount}, RequestedCount={RequestedCount}", 
                        searchAttempt, currentSimilarityThreshold, projectsWithScores.Count, requestedCount);

                    // Calculate how many more projects we need
                    var remainingNeeded = requestedCount - projectsWithScores.Count;
                    var searchSize = Math.Max(remainingNeeded * 3, 15); // Get 3x more than needed to give AI more options

                    // Run vector search with current threshold and get scores
                    var matchedProjectIdsWithScores = await runVectorSearchWithScoresAsync(embedding, searchSize, currentSimilarityThreshold);
                    
                    // Filter out already processed project IDs
                    var newProjectIdsWithScores = matchedProjectIdsWithScores.Where(kvp => !allProcessedProjectIds.Contains(kvp.Item1)).ToList();
                    
                    if (newProjectIdsWithScores.Count == 0)
                    {
                        r_logger.LogInformation("No new project IDs found in search attempt {Attempt}", searchAttempt);
                        break; // No more projects to process
                    }

                    // Add new IDs to processed set
                    foreach (var kvp in newProjectIdsWithScores)
                    {
                        allProcessedProjectIds.Add(kvp.Item1);
                    }

                    r_logger.LogInformation("Found {NewCount} new project IDs in search attempt {Attempt}", newProjectIdsWithScores.Count, searchAttempt);

                    // Fetch projects for new IDs
                    var newProjectIds = newProjectIdsWithScores.Select(kvp => kvp.Item1).ToList();
                    var newProjects = await fetchProjectsByIdsAsync(newProjectIds);
                    r_logger.LogInformation("Fetched {FetchedCount} new projects", newProjects.Count);

                    // Apply chat filtering to new projects
                    var filteredNewProjects = await filterProjectsWithChatAsync(query, newProjects, requestedCount);
                    r_logger.LogInformation("Chat filtered {FilteredCount} new projects", filteredNewProjects.Count);

                    // Add filtered projects with their scores (avoid duplicates)
                    var existingProjectIds = new HashSet<Guid>(projectsWithScores.Select(p => p.Project.ProjectId));
                    foreach (var project in filteredNewProjects)
                    {
                        if (!existingProjectIds.Contains(project.ProjectId))
                        {
                            // Find the score for this project
                            var score = newProjectIdsWithScores.FirstOrDefault(kvp => kvp.Item1 == project.ProjectId).Item2;
                            projectsWithScores.Add((project, score));
                        }
                    }
                    
                    r_logger.LogInformation("Added {AddedCount} unique projects. Total count now: {TotalCount}", 
                        filteredNewProjects.Count(p => !existingProjectIds.Contains(p.ProjectId)), projectsWithScores.Count);

                    // If we have enough projects, break
                    if (projectsWithScores.Count >= requestedCount)
                    {
                        r_logger.LogInformation("Reached requested count after {Attempt} search attempts", searchAttempt);
                        break;
                    }

                    // Prepare for next iteration with lower threshold
                    currentSimilarityThreshold -= thresholdDecrement;
                    searchAttempt++;

                    // Ensure threshold doesn't go too low
                    if (currentSimilarityThreshold < 0.3)
                    {
                        r_logger.LogWarning("Similarity threshold reached minimum (0.3). Stopping search attempts.");
                        break;
                    }
                }

                // Sort by score (descending) and take exactly the requested count
                var result = projectsWithScores
                    .OrderByDescending(p => p.Score)
                    .Take(requestedCount)
                    .Select(p => p.Project)
                    .ToList();
                
                var duration = DateTime.UtcNow - startTime;
                r_logger.LogInformation("Iterative search completed: FinalCount={FinalCount}, RequestedCount={RequestedCount}, Attempts={Attempts}, Duration={Duration}ms",
                    result.Count, requestedCount, searchAttempt, duration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                r_logger.LogError(ex, "Error in iterative search: RequestedCount={RequestedCount}, Duration={Duration}ms", requestedCount, duration.TotalMilliseconds);
                throw;
            }
        }

        private async Task<List<TemplateProject>> fetchProjectsByIdsAsync(List<Guid> matchedProjectIds)
        {
            r_logger.LogInformation("Fetching projects by IDs: ProjectIds={ProjectIds}, Count={Count}",
                string.Join(",", matchedProjectIds), matchedProjectIds.Count);

            try
            {
                // Remove duplicates from the input list to avoid unnecessary database queries
                var uniqueProjectIds = matchedProjectIds.Distinct().ToList();
                r_logger.LogInformation("Unique project IDs after deduplication: UniqueCount={UniqueCount}, OriginalCount={OriginalCount}",
                    uniqueProjectIds.Count, matchedProjectIds.Count);

                // Log each project ID we're looking for
                foreach (var projectId in uniqueProjectIds)
                {
                    r_logger.LogInformation("Looking for project ID: {ProjectId}", projectId);
                }

                List<TemplateProject> templateProjects = await r_DbContext.TemplateProjects
                    .Where(p => uniqueProjectIds.Contains(p.ProjectId))
                    .ToListAsync();

                List<UserProject> userProjects = await r_DbContext.Projects
                    .Where(p => uniqueProjectIds.Contains(p.ProjectId))
                    .ToListAsync();

                r_logger.LogInformation("Database check: TotalTemplateProjects={TotalTemplateProjects}, TotalUserProjects={TotalUserProjects}",
                    await r_DbContext.TemplateProjects.CountAsync(), await r_DbContext.Projects.CountAsync());

                r_logger.LogInformation("Project ID matches: FoundInTemplates={FoundInTemplates}, FoundInUsers={FoundInUsers}",
                    templateProjects.Count, userProjects.Count);

                // Log which specific projects were found
                foreach (var project in templateProjects)
                {
                    r_logger.LogInformation("Found template project: {ProjectId} - {ProjectName}", project.ProjectId, project.ProjectName);
                }

                foreach (var project in userProjects)
                {
                    r_logger.LogInformation("Found user project: {ProjectId} - {ProjectName}", project.ProjectId, project.ProjectName);
                }

                Dictionary<Guid, TemplateProject> mergedProjects = new();

                foreach (var userProject in userProjects)
                {
                    mergedProjects[userProject.ProjectId] = userProject;
                }

                foreach (var templateProject in templateProjects)
                {
                    if (!mergedProjects.ContainsKey(templateProject.ProjectId))
                    {
                        mergedProjects[templateProject.ProjectId] = templateProject;
                    }
                }

                var result = mergedProjects.Values.ToList();
                r_logger.LogInformation("Projects fetched successfully: RequestedCount={RequestedCount}, FetchedCount={FetchedCount}",
                    uniqueProjectIds.Count, result.Count);
                return result;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error fetching projects by IDs: ProjectIds={ProjectIds}, Count={Count}",
                    string.Join(",", matchedProjectIds), matchedProjectIds.Count);
                throw;
            }
        }


        private EnhancedProjectSearchViewModel ConvertToEnhancedProjectSearchViewModel(TemplateProject project)
        {
            var viewModel = new EnhancedProjectSearchViewModel
            {
                ProjectId = project.ProjectId.ToString(),
                ProjectName = project.ProjectName,
                ProjectDescription = project.ProjectDescription,
                ProjectPictureUrl = project.ProjectPictureUrl, // This should be available in the model
                DifficultyLevel = project.DifficultyLevel.ToString(),
                DurationDays = (int)Math.Round(project.Duration.TotalDays),
                DurationText = HumanizeDays((int)Math.Round(project.Duration.TotalDays)), // Convert to human-readable format
                RequiredRoles = project.RequiredRoles?.ToArray() ?? new string[0],
                Technologies = project.Technologies?.ToArray() ?? new string[0],
                Goals = project.Goals?.ToArray() ?? new string[0],
                ProgrammingLanguages = new string[0] // Default for TemplateProject
            };

            // For templates, open roles are the same as required roles
            viewModel.OpenRoles = viewModel.RequiredRoles;

            // If it's a UserProject, populate the additional fields
            if (project is UserProject userProject)
            {
                viewModel.ProjectSource = userProject.ProjectSource.ToString();
                viewModel.ProjectStatus = userProject.ProjectStatus.ToString();
                viewModel.ProgrammingLanguages = userProject.ProgrammingLanguages?.ToArray() ?? new string[0];
                viewModel.RepositoryLink = userProject.RepositoryLink;
                viewModel.TeamSize = userProject.ProjectMembers?.Count ?? 0;
                
                // For user projects, calculate open roles (roles that still need team members)
                viewModel.OpenRoles = CalculateOpenRoles(userProject);
            }
            else
            {
                // For TemplateProject, set these as default values
                viewModel.ProjectSource = "Template";
                viewModel.ProjectStatus = eProjectStatus.NotActive.ToString();
                viewModel.TeamSize = 0;
            }

            return viewModel;
        }

        private static string HumanizeDays(int days)
        {
            const int daysInYear = 365;
            const int daysInMonth = 30;
            const int daysInWeek = 7;

            if (days <= 0)
            {
                return "0 days";
            }

            if (days % daysInYear == 0)
            {
                int years = days / daysInYear;
                return years == 1 ? "1 year" : $"{years} years";
            }

            if (days % daysInMonth == 0)
            {
                int months = days / daysInMonth;
                return months == 1 ? "1 month" : $"{months} months";
            }

            if (days % daysInWeek == 0)
            {
                int weeks = days / daysInWeek;
                return weeks == 1 ? "1 week" : $"{weeks} weeks";
            }

            return days == 1 ? "1 day" : $"{days} days";
        }

        private string[] CalculateOpenRoles(UserProject userProject)
        {
            // For consistency with other view models, just return RequiredRoles
            // The frontend can handle role availability logic if needed
            return userProject.RequiredRoles?.ToArray() ?? new string[0];
        }
    }
}