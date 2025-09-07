using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using GainIt.API.Data;
using GainIt.API.DTOs.Search;
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
        private const double r_similarityThreshold = 0.80;

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

        public async Task<ProjectMatchResultDto> MatchProjectsByTextAsync(string i_InputText, int i_ResultCount = 3)
        {
            r_logger.LogInformation("Matching projects by text: InputText={InputText}, ResultCount={ResultCount}", i_InputText, i_ResultCount);

            try
            {
                var chatrefinedQuery = await refineQueryWithChatAsync(i_InputText);
                r_logger.LogInformation("Query refined with chat: OriginalQuery={OriginalQuery}, RefinedQuery={RefinedQuery}", i_InputText, chatrefinedQuery);

                var embedding = await getEmbeddingAsync(chatrefinedQuery);
                r_logger.LogInformation("Embedding generated: EmbeddingSize={EmbeddingSize}", embedding.Count);

                var matchedProjectIds = await runVectorSearchAsync(embedding, i_ResultCount);
                r_logger.LogInformation("Vector search completed: MatchedProjectIds={MatchedProjectIds}, Count={Count}", string.Join(",", matchedProjectIds), matchedProjectIds.Count);

                var matchedProjects = await fetchProjectsByIdsAsync(matchedProjectIds);
                r_logger.LogInformation("Projects fetched by IDs: FetchedCount={FetchedCount}", matchedProjects.Count);

                var filteredProjects = await filterProjectsWithChatAsync(chatrefinedQuery, matchedProjects);
                r_logger.LogInformation("Projects filtered with chat: FilteredCount={FilteredCount}", filteredProjects.Count);

                // Convert to AzureVectorSearchProjectViewModel
                var projectViewModels = filteredProjects.Select(ConvertToAzureVectorSearchViewModel).ToList();

                var chatExplenation = await getChatExplanationAsync(chatrefinedQuery, filteredProjects);
                r_logger.LogInformation("Chat explanation generated: Chat Explanation={Explanation}", chatExplenation);

                var result = new ProjectMatchResultDto(projectViewModels, chatExplenation);
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

        public async Task<IEnumerable<AzureVectorSearchProjectViewModel>> MatchProjectsByProfileAsync(Guid i_UserId, int i_ResultCount = 3)
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

                var matchedProjectIds = await runVectorSearchAsync(embedding, i_ResultCount);
                r_logger.LogInformation("Profile vector search completed: UserId={UserId}, MatchedProjectIds={MatchedProjectIds}, Count={Count}", i_UserId, string.Join(",", matchedProjectIds), matchedProjectIds.Count);

                var matchedProjects = await fetchProjectsByIdsAsync(matchedProjectIds);
                r_logger.LogInformation("Profile projects fetched: UserId={UserId}, FetchedCount={FetchedCount}", i_UserId, matchedProjects.Count);

                var filteredProjects = await filterProjectsWithChatAsync(chatrefinedQuery, matchedProjects);
                // Replace the existing logging line with the following to log project names as well:
                r_logger.LogInformation(
                    "Profile projects filtered: UserId={UserId}, FilteredCount={FilteredCount}, ProjectNames={ProjectNames}",
                    i_UserId,
                    filteredProjects.Count,
                    string.Join(", ", filteredProjects.Select(p => $"\u001b[32m{p.ProjectName}\u001b[0m"))
                );

                // Convert to AzureVectorSearchProjectViewModel
                var projectViewModels = filteredProjects.Select(ConvertToAzureVectorSearchViewModel).ToList();
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
                        "You are an assistant that explains *exactly* why each suggested project matches the user's query. " +
                        "Follow these rules strictly:\n" +
                        "1. Refer to **all** provided fields: Name, Description, Goals, Technologies, Difficulty, Duration, Source, and Status (if present).\n" +
                        "2. Give **1–2 bullet points** per project, **max 20 words** each, in the form:\n" +
                        "   - ProjectName: [point]\n" +
                        "3. Do **not** invent or suggest any projects beyond the list.\n" +
                        "4. If Source is `NonprofitOrganization`, you may note its real-world context; otherwise omit source and status.\n" +
                        "5. Focus each bullet on **how specific attributes** of the project satisfy the user's query.\n" +
                        "6. If a project's Status is `InProgress`, you may **optionally** mention one benefit of joining an ongoing project—but **sparingly**:\n" +
                        "   only when it truly adds relevance, and **do not** include this bullet for every active project."
                    ),
                    new UserChatMessage(
                        $"User query: {i_Query}\n\nProjects:\n{summaries}\n\n" +
                        "Explain each project's relevance:")
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

        private async Task<List<TemplateProject>> filterProjectsWithChatAsync(string i_Query, List<TemplateProject> i_Projects)
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
                    "Review the projects and return a JSON array of the IDs (as strings) of all the projects that are clearly relevant. " +
                    "Include all that are a good match — do not exclude any just to reduce the list. " +
                    "If all are relevant, return them all. If none are relevant, return an empty array []."),
                new UserChatMessage(
                    $"Query: {i_Query}\n\nProjects:\n{summaries}")
                };

                var options = new ChatCompletionOptions
                {
                    Temperature = 0f
                };

                ChatCompletion completion =
                    await r_chatClient.CompleteChatAsync(messages, options);
                var response = completion.Content[0].Text.Trim();

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

                if (response == "none")
                {
                    r_logger.LogInformation("No projects matched the query: Query={Query}", i_Query);
                    return new List<TemplateProject>();
                }



                try
                {
                    var stringIds = JsonSerializer.Deserialize<string[]>(response);
                    projectIds = stringIds?.Select(Guid.Parse).ToArray() ?? Array.Empty<Guid>();
                }
                catch
                {
                    projectIds = Array.Empty<Guid>();
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

        private async Task<List<Guid>> runVectorSearchAsync(IReadOnlyList<float> embedding, int resultCount)
        {
            r_logger.LogInformation("Running vector search: EmbeddingSize={EmbeddingSize}, ResultCount={ResultCount}", embedding.Count, resultCount);
            var startTime = DateTime.UtcNow;

            try
            {
                var searchOptions = new SearchOptions
                {
                    Size = resultCount,
                    VectorSearch = new VectorSearchOptions
                    {
                        Queries =
                        {
                            new VectorizedQuery(embedding.ToArray())
                            {
                                KNearestNeighborsCount = resultCount,
                                Fields = { "text_vector" }
                            }
                        }
                    },
                    Select = { "projectId" }
                };

                var results = await r_searchClient.SearchAsync<ProjectSearchResult>(null, searchOptions);
                var matchedProjectIds = new List<Guid>();

                await foreach (var result in results.Value.GetResultsAsync())
                {
                    if (result.Score.HasValue && result.Score.Value >= r_similarityThreshold)
                    {
                        // Log what we're getting for projectId
                        r_logger.LogInformation("Found search result with score {Score}, ProjectId: '{ProjectId}'", 
                            result.Score.Value, result.Document.projectId ?? "NULL");
                        
                        // Parse the string ProjectId to Guid
                        if (Guid.TryParse(result.Document.projectId, out Guid projectId))
                        {
                            matchedProjectIds.Add(projectId);
                        }
                        else
                        {
                            r_logger.LogWarning("Failed to parse ProjectId: '{ProjectId}'", result.Document.projectId ?? "NULL");
                        }
                    }
                }

                var duration = DateTime.UtcNow - startTime;
                r_logger.LogInformation("Vector search completed: MatchedProjectIds={MatchedProjectIds}, Count={Count}, Duration={Duration}ms",
                    string.Join(",", matchedProjectIds), matchedProjectIds.Count, duration.TotalMilliseconds);
                return matchedProjectIds;
            }
            catch (Exception ex)
            {
                var duration = DateTime.UtcNow - startTime;
                r_logger.LogError(ex, "Error running vector search: EmbeddingSize={EmbeddingSize}, ResultCount={ResultCount}, Duration={Duration}ms", embedding.Count, resultCount, duration.TotalMilliseconds);
                throw;
            }
        }

        private async Task<List<TemplateProject>> fetchProjectsByIdsAsync(List<Guid> matchedProjectIds)
        {
            r_logger.LogInformation("Fetching projects by IDs: ProjectIds={ProjectIds}, Count={Count}",
                string.Join(",", matchedProjectIds), matchedProjectIds.Count);

            try
            {
                List<TemplateProject> templateProjects = await r_DbContext.TemplateProjects
                    .Where(p => matchedProjectIds.Contains(p.ProjectId))
                    .ToListAsync();

                List<UserProject> userProjects = await r_DbContext.Projects
                    .Where(p => matchedProjectIds.Contains(p.ProjectId))
                    .ToListAsync();

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
                    matchedProjectIds.Count, result.Count);
                return result;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error fetching projects by IDs: ProjectIds={ProjectIds}, Count={Count}",
                    string.Join(",", matchedProjectIds), matchedProjectIds.Count);
                throw;
            }
        }

        /// <summary>
        /// Converts a TemplateProject (which can be either TemplateProject or UserProject) to AzureVectorSearchProjectViewModel
        /// </summary>
        /// <param name="project">The project to convert</param>
        /// <returns>AzureVectorSearchProjectViewModel with all relevant fields populated</returns>
        private AzureVectorSearchProjectViewModel ConvertToAzureVectorSearchViewModel(TemplateProject project)
        {
            var viewModel = new AzureVectorSearchProjectViewModel
            {
                ProjectId = project.ProjectId.ToString(),
                ProjectName = project.ProjectName,
                ProjectDescription = project.ProjectDescription,
                DifficultyLevel = project.DifficultyLevel.ToString(),
                DurationDays = (int)project.Duration.TotalDays,
                Goals = project.Goals?.ToArray() ?? new string[0],
                Technologies = project.Technologies?.ToArray() ?? new string[0],
                RequiredRoles = project.RequiredRoles?.ToArray() ?? new string[0],
                ProgrammingLanguages = project.ProgrammingLanguages?.ToArray() ?? new string[0],
                RagContext = new RagContextViewModel
                {
                    SearchableText = project.RagContext?.SearchableText ?? string.Empty,
                    Tags = project.RagContext?.Tags?.ToArray() ?? new string[0],
                    SkillLevels = project.RagContext?.SkillLevels?.ToArray() ?? new string[0],
                    ProjectType = project.RagContext?.ProjectType ?? string.Empty,
                    Domain = project.RagContext?.Domain ?? string.Empty,
                    LearningOutcomes = project.RagContext?.LearningOutcomes?.ToArray() ?? new string[0],
                    ComplexityFactors = project.RagContext?.ComplexityFactors?.ToArray() ?? new string[0]
                }
            };

            // If it's a UserProject, populate the additional fields
            if (project is UserProject userProject)
            {
                viewModel.ProjectSource = userProject.ProjectSource.ToString();
                viewModel.ProjectStatus = userProject.ProjectStatus.ToString();
            }
            else
            {
                // For TemplateProject, set these as null or default values
                viewModel.ProjectSource = "Template";
                viewModel.ProjectStatus = null;
            }

            return viewModel;
        }
    }
}