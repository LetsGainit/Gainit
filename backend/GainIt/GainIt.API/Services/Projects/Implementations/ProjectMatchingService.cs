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


namespace GainIt.API.Services.Projects.Implementations
{
    public class ProjectMatchingService : IProjectMatchingService
    {
        private readonly SearchClient r_searchClient;
        private readonly AzureOpenAIClient r_azureOpenAIClient;
        private readonly string r_embeddingModelName;
        private readonly string r_chatModelName;
        private readonly GainItDbContext r_DbContext;
        private const double r_similarityThreshold = 0.80;

        public ProjectMatchingService(SearchClient i_SearchClient, AzureOpenAIClient i_AzureOpenAIClient, IOptions<OpenAIOptions> i_OpenAIOptionsAccessor, GainItDbContext i_DbContext)
        {
            r_searchClient = i_SearchClient;
            r_azureOpenAIClient = i_AzureOpenAIClient;
            r_embeddingModelName = i_OpenAIOptionsAccessor.Value.EmbeddingDeploymentName;
            r_chatModelName = i_OpenAIOptionsAccessor.Value.ChatDeploymentName;
            r_DbContext = i_DbContext;
        }

        public async Task<IEnumerable<TemplateProject>> MatchProjectsByTextAsync(string i_InputText, int i_ResultCount = 3)
        {
            var chatrefinedQuery = await refineQueryWithChatAsync(i_InputText);
            var embedding = await getEmbeddingAsync(chatrefinedQuery);
            var matchedProjectIds = await runVectorSearchAsync(embedding, i_ResultCount);
            var matchedProjects = await fetchProjectsByIdsAsync(matchedProjectIds);
            var filteredProjects = await filterProjectsWithChatAsync(chatrefinedQuery, matchedProjects);
            var chatExplenation = await getChatExplanationAsync(chatrefinedQuery);

            return allProjects;
        }

        public async Task<IEnumerable<TemplateProject>> MatchProjectsByProfileAsync(User i_UserProfile, int i_ResultCount = 3)
        {
            string searchquery = buildProfileQuery(i_UserProfile);
            var chatrefinedQuery = await refineQueryWithChatAsync(searchquery);
            var embedding = await getEmbeddingAsync(chatrefinedQuery);
            var matchedProjectIds = await runVectorSearchAsync(embedding, i_ResultCount);
            var matchedProjects = await fetchProjectsByIdsAsync(matchedProjectIds);
            var filteredProjects = await filterProjectsWithChatAsync(chatrefinedQuery, matchedProjects);

            return filteredProjects;
        }

        private async Task<string> getChatExplanationAsync(string i_Query)
        {
           
        }

        private async Task<string> refineQueryWithChatAsync(string i_OriginalQuery)
        {

        }

        private async Task<List<TemplateProject>> filterProjectsWithChatAsync(string i_Query, List<TemplateProject> i_Projects)
        {

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