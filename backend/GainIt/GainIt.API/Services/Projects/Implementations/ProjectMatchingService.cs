using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using GainIt.API.Data;
using GainIt.API.DTOs;
using GainIt.API.DTOs.Search;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;
using GainIt.API.Options;
using GainIt.API.Services.Projects.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GainIt.API.Services.Projects.Implementations
{
    public class ProjectMatchingService : IProjectMatchingService
    {
        private readonly SearchClient r_searchClient;
        private readonly OpenAIClient r_openAIClient;
        private readonly string r_embeddingModelName;
        private readonly GainItDbContext r_DbContext;
        private const double r_similarityThreshold = 0.83;

        public ProjectMatchingService(SearchClient i_SearchClient, OpenAIClient i_OpenAIClient, IOptions<OpenAIOptions> i_OpenAIOptionsAccessor, GainItDbContext i_DbContext)
        {
            r_searchClient = i_SearchClient;
            r_openAIClient = i_OpenAIClient;
            r_embeddingModelName = i_OpenAIOptionsAccessor.Value.EmbeddingDeploymentName;
            r_DbContext = i_DbContext;
        }

        public async Task<IEnumerable<TemplateProject>> MatchProjectsByFreeTextAsync(string inputText, int resultCount = 3)
        {
            var embedding = await GetEmbeddingAsync(inputText);
            var matchedProjectIds = await RunVectorSearchAsync(embedding, resultCount);
            var allProjects = await FetchProjectsByIdsAsync(matchedProjectIds);
            return allProjects;
        }

        public async Task<string> MatchWithProfileAndExplainAsync(User i_UserProfile, int i_ResultCount = 3)
        {
            return await Task.Run(() =>
            {
                return $"Matching projects for user {i_UserProfile.UserId} with {i_ResultCount} results.";
            });
        }

        private async Task<IReadOnlyList<float>> GetEmbeddingAsync(string inputText)
        {
            var embeddingOptions = new EmbeddingsOptions(r_embeddingModelName, new List<string> { inputText });
            var embeddingResponse = await r_openAIClient.GetEmbeddingsAsync(embeddingOptions);
            return embeddingResponse.Value.Data.First().Embedding.ToArray();
        }

        private async Task<List<Guid>> RunVectorSearchAsync(IReadOnlyList<float> embedding, int resultCount)
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

        private async Task<List<TemplateProject>> FetchProjectsByIdsAsync(List<Guid> matchedProjectIds)
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
