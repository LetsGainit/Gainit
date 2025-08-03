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
        private const double r_similarityThreshold = 0.80;

        public ProjectMatchingService(SearchClient i_SearchClient, AzureOpenAIClient i_AzureOpenAIClient, IOptions<OpenAIOptions> i_OpenAIOptionsAccessor, GainItDbContext i_DbContext)
        {
            r_searchClient = i_SearchClient;
            r_azureOpenAIClient = i_AzureOpenAIClient;
            r_embeddingModelName = i_OpenAIOptionsAccessor.Value.EmbeddingDeploymentName;
            r_chatClient = i_AzureOpenAIClient.GetChatClient(i_OpenAIOptionsAccessor.Value.ChatDeploymentName);
            r_chatModelName = i_OpenAIOptionsAccessor.Value.ChatDeploymentName;
            r_DbContext = i_DbContext;
        }

        public async Task<ProjectMatchResultDto> MatchProjectsByTextAsync(string i_InputText, int i_ResultCount = 3)
        {
            var chatrefinedQuery = await refineQueryWithChatAsync(i_InputText);
            var embedding = await getEmbeddingAsync(chatrefinedQuery);
            var matchedProjectIds = await runVectorSearchAsync(embedding, i_ResultCount);
            var matchedProjects = await fetchProjectsByIdsAsync(matchedProjectIds);
            var filteredProjects = await filterProjectsWithChatAsync(chatrefinedQuery, matchedProjects);
            var chatExplenation = await getChatExplanationAsync(chatrefinedQuery, filteredProjects);

            return new ProjectMatchResultDto(filteredProjects, chatExplenation);
        }

        public async Task<IEnumerable<TemplateProject>> MatchProjectsByProfileAsync(Guid i_UserId, int i_ResultCount = 3)
        {
            var userProfile = await r_DbContext.Users
                .FirstOrDefaultAsync(u => u.UserId == i_UserId);

            if (userProfile == null)
            {
                throw new KeyNotFoundException("User profile not found.");
            }

            string searchquery = buildProfileQuery(userProfile);
            var chatrefinedQuery = await refineQueryWithChatAsync(searchquery);
            var embedding = await getEmbeddingAsync(chatrefinedQuery);
            var matchedProjectIds = await runVectorSearchAsync(embedding, i_ResultCount);
            var matchedProjects = await fetchProjectsByIdsAsync(matchedProjectIds);
            var filteredProjects = await filterProjectsWithChatAsync(chatrefinedQuery, matchedProjects);

            return filteredProjects;
        }

        private async Task<string> getChatExplanationAsync(string i_Query, List<TemplateProject> i_MatchedProjects)
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
                    "6. If a project’s Status is `InProgress`, you may **optionally** mention one benefit of joining an ongoing project—but **sparingly**:\n" +
                    "   only when it truly adds relevance, and **do not** include this bullet for every active project."
                ),
                new UserChatMessage(
                    $"User query: {i_Query}\n\nProjects:\n{summaries}\n\n" +
                    "Explain each project’s relevance:")
            };

            var options = new ChatCompletionOptions
            {
                Temperature = 0f
            };

            ChatCompletion completion =
                await r_chatClient.CompleteChatAsync(messages, options);

            return completion.Content[0].Text.Trim();
        }

        private async Task<string> refineQueryWithChatAsync(string i_OriginalQuery)
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
            return completion.Content[0].Text.Trim();
        }

        private async Task<List<TemplateProject>> filterProjectsWithChatAsync(string i_Query, List<TemplateProject> i_Projects)
        {
            if (i_Projects == null || !i_Projects.Any())
                return new List<TemplateProject>();

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

            try
            {
                var stringIds = JsonSerializer.Deserialize<string[]>(response);
                projectIds = stringIds.Select(Guid.Parse).ToArray();
            }
            catch
            {
                projectIds = Array.Empty<Guid>();
            }

            return i_Projects
                .Where(p => projectIds.Contains(p.ProjectId))
                .ToList();
        }

        private string buildProfileQuery(User i_userProfile)
        {
            if (i_userProfile is Gainer gainer)
            {
                var parts = new List<string>
                {
                    gainer.Biography,
                    gainer.EducationStatus,
                    string.Join(", ", gainer.AreasOfInterest ?? new List<string>())
                };
                if (gainer.TechExpertise != null)
                {
                    parts.Add(gainer.TechExpertise.ToString());
                }
                return string.Join(". ", parts);
            }
            else if (i_userProfile is Mentor mentor)
            {
                var parts = new List<string>
                {
                    mentor.Biography,
                    mentor.AreaOfExpertise,
                    $"{mentor.YearsOfExperience} years of experience"
                };
                if (mentor.TechExpertise != null)
                {
                    parts.Add(mentor.TechExpertise.ToString());
                }
                return string.Join(". ", parts);
            }
            else
            {
                throw new ArgumentException("Profile type is not supported for project matching.");
            }
        }

        private async Task<IReadOnlyList<float>> getEmbeddingAsync(string inputText)
        {
            var embeddingClient = r_azureOpenAIClient.GetEmbeddingClient(r_embeddingModelName);
            var embeddingResult = await embeddingClient.GenerateEmbeddingAsync(inputText);
            return embeddingResult.Value.ToFloats().ToArray();
        }

        private async Task<List<Guid>> runVectorSearchAsync(IReadOnlyList<float> embedding, int resultCount)
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
                Select = { "ProjectId" }
            };

            var results = await r_searchClient.SearchAsync<ProjectSearchResult>(null, searchOptions);
            var matchedProjectIds = new List<Guid>();

            await foreach (var result in results.Value.GetResultsAsync())
            {
                if (result.Score.HasValue && result.Score.Value >= r_similarityThreshold)
                {
                    matchedProjectIds.Add(result.Document.ProjectId);
                }
            }

            return matchedProjectIds;
        }

        private async Task<List<TemplateProject>> fetchProjectsByIdsAsync(List<Guid> matchedProjectIds)
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

            return mergedProjects.Values.ToList();
        }
    }
}