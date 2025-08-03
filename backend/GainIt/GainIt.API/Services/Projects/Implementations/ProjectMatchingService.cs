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

                var chatExplenation = await getChatExplanationAsync(chatrefinedQuery, filteredProjects);
                r_logger.LogInformation("Chat explanation generated: ExplanationLength={ExplanationLength}", chatExplenation.Length);

                var result = new ProjectMatchResultDto(filteredProjects, chatExplenation);
                r_logger.LogInformation("Project matching completed successfully: FinalResultCount={FinalResultCount}", filteredProjects.Count);
                return result;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error matching projects by text: InputText={InputText}, ResultCount={ResultCount}", i_InputText, i_ResultCount);
                throw;
            }
        }

        public async Task<IEnumerable<TemplateProject>> MatchProjectsByProfileAsync(Guid i_UserId, int i_ResultCount = 3)
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
                r_logger.LogInformation("Profile projects filtered: UserId={UserId}, FilteredCount={FilteredCount}", i_UserId, filteredProjects.Count);

                return filteredProjects;
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
                        "You are an assistant that filters projects based on user queries. " +
                        "Return ONLY the ProjectIds of projects that match the user's query, separated by commas. " +
                        "If no projects match, return 'none'. " +
                        "Be selective and only include projects that truly match the user's needs."
                    ),
                    new UserChatMessage(
                        $"User query: {i_Query}\n\nProjects:\n{summaries}\n\n" +
                        "Return only the ProjectIds of matching projects (comma-separated):")
                };

                var options = new ChatCompletionOptions
                {
                    Temperature = 0f
                };

                ChatCompletion completion = await r_chatClient.CompleteChatAsync(messages, options);
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

                response = response.ToLower();

                if (response == "none")
                {
                    r_logger.LogInformation("No projects matched the query: Query={Query}", i_Query);
                    return new List<TemplateProject>();
                }

                Guid[] projectIds;

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
            r_logger.LogInformation("Building profile query: UserId={UserId}, UserType={UserType}", i_userProfile.UserId, i_userProfile.GetType().Name);

            try
            {
                var query = "";
                

                if (i_userProfile is Gainer gainer)
                {
                    query = $"Gainer profile: {gainer.FullName}, Education: {gainer.EducationStatus}, " +
                           $"Areas of Interest: [{string.Join(", ", gainer.AreasOfInterest ?? new List<string>())}], " +
                           $"Biography: {gainer.Biography}";

                    if (gainer.TechExpertise != null)
                    {
                        query += $", Programming Languages: [{string.Join(", ", gainer.TechExpertise.ProgrammingLanguages ?? new List<string>())}], " +
                                $"Technologies: [{string.Join(", ", gainer.TechExpertise.Technologies ?? new List<string>())}], " +
                                $"Tools: [{string.Join(", ", gainer.TechExpertise.Tools ?? new List<string>())}]";
                    }
                }
                                 else if (i_userProfile is Mentor mentor)
                 {
                     query = $"Mentor profile: {mentor.FullName}, Years of Experience: {mentor.YearsOfExperience}, " +
                            $"Area of Expertise: {mentor.AreaOfExpertise}, " +
                            $"Biography: {mentor.Biography}";

                    if (mentor.TechExpertise != null)
                    {
                        query += $", Programming Languages: [{string.Join(", ", mentor.TechExpertise.ProgrammingLanguages ?? new List<string>())}], " +
                                $"Technologies: [{string.Join(", ", mentor.TechExpertise.Technologies ?? new List<string>())}], " +
                                $"Tools: [{string.Join(", ", mentor.TechExpertise.Tools ?? new List<string>())}]";
                    }
                }
                                 else if (i_userProfile is GainIt.API.Models.Users.Nonprofits.NonprofitOrganization nonprofit)
                 {
                     query = $"Nonprofit profile: {nonprofit.FullName}, Website: {nonprofit.WebsiteUrl}, " +
                            $"Biography: {nonprofit.Biography}";

                    if (nonprofit.NonprofitExpertise != null)
                    {
                        query += $", Field of Work: {nonprofit.NonprofitExpertise.FieldOfWork}, " +
                                $"Mission Statement: {nonprofit.NonprofitExpertise.MissionStatement}";
                    }
                }

                r_logger.LogInformation("Profile query built successfully: UserId={UserId}, QueryLength={QueryLength}", i_userProfile.UserId, query.Length);
                return query;
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
                    Select = { "ProjectId", "ProjectName", "ProjectDescription" }
                };

                var searchResults = await r_searchClient.SearchAsync<SearchDocument>("*", searchOptions);
                var matchedProjectIds = new List<Guid>();

                await foreach (SearchResult<SearchDocument> result in searchResults.Value.GetResultsAsync())
                {
                    if (result.Document.TryGetValue("ProjectId", out var projectIdObj) && projectIdObj != null)
                    {
                        if (Guid.TryParse(projectIdObj.ToString(), out var projectId))
                        {
                            matchedProjectIds.Add(projectId);
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
                var projects = await r_DbContext.TemplateProjects
                    .Where(p => matchedProjectIds.Contains(p.ProjectId))
                    .ToListAsync();

                var userProjects = await r_DbContext.Projects
                    .Where(p => matchedProjectIds.Contains(p.ProjectId))
                    .ToListAsync();

                var allProjects = new List<TemplateProject>();
                allProjects.AddRange(projects);
                allProjects.AddRange(userProjects.Cast<TemplateProject>());

                r_logger.LogInformation("Projects fetched successfully: RequestedCount={RequestedCount}, FetchedCount={FetchedCount}", 
                    matchedProjectIds.Count, allProjects.Count);
                return allProjects;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error fetching projects by IDs: ProjectIds={ProjectIds}, Count={Count}", 
                    string.Join(",", matchedProjectIds), matchedProjectIds.Count);
                throw;
            }
        }
    }
}