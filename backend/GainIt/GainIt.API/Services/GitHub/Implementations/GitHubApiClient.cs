using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GainIt.API.Models.Projects;
using GainIt.API.Options;
using GainIt.API.Services.GitHub.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GainIt.API.Services.GitHub.Implementations
{
    public class GitHubApiClient : IGitHubApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IOptions<GitHubOptions> _options;
        private readonly ILogger<GitHubApiClient> _logger;
        private readonly SemaphoreSlim _rateLimitSemaphore;
        private DateTime _rateLimitResetAt = DateTime.UtcNow.AddHours(1);
        private int _remainingRequests = 5000;

        public GitHubApiClient(
            HttpClient httpClient,
            IOptions<GitHubOptions> options,
            ILogger<GitHubApiClient> logger)
        {
            _httpClient = httpClient;
            _options = options;
            _logger = logger;
            _rateLimitSemaphore = new SemaphoreSlim(1, 1);

            // Configure HTTP client
            _httpClient.BaseAddress = new Uri(_options.Value.GraphQLEndpoint);
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.Value.RequestTimeoutSeconds);
        }

        public async Task<GitHubRepositoryNode?> GetRepositoryAsync(string owner, string name)
        {
            var query = @"
                query($owner: String!, $name: String!) {
                    repository(owner: $owner, name: $name) {
                        id
                        name
                        nameWithOwner
                        description
                        url
                        isPrivate
                        isArchived
                        isFork
                        defaultBranchRef {
                            name
                        }
                        primaryLanguage {
                            name
                            color
                        }
                        languages(first: 10) {
                            nodes {
                                name
                                color
                            }
                            totalCount
                        }
                        licenseInfo {
                            name
                            url
                        }
                        stargazerCount
                        forkCount
                        watchers(first: 1) {
                            totalCount
                        }
                        issues(first: 1) {
                            totalCount
                        }
                        pullRequests(first: 1) {
                            totalCount
                        }
                        refs(first: 1) {
                            totalCount
                        }
                        releases(first: 1) {
                            totalCount
                        }
                        createdAt
                        updatedAt
                        pushedAt
                    }
                }";

            var variables = new { owner, name };
            var response = await ExecuteGraphQLQueryAsync<GitHubRepositoryData>(query, variables);
            return response?.Repository;
        }

        public async Task<GitHubAnalyticsRepository?> GetRepositoryAnalyticsAsync(string owner, string name, int daysPeriod = 30)
        {
            var since = DateTime.UtcNow.AddDays(-daysPeriod).ToString("yyyy-MM-ddTHH:mm:ssZ");

            var query = @"
                query($owner: String!, $name: String!, $since: GitTimestamp!) {
                    repository(owner: $owner, name: $name) {
                        defaultBranchRef {
                            target {
                                ... on Commit {
                                    history(since: $since) {
                                        totalCount
                                        nodes {
                                            id
                                            message
                                            committedDate
                                            author {
                                                name
                                                email
                                                user {
                                                    login
                                                }
                                            }
                                            additions
                                            deletions
                                            changedFiles
                                        }
                                    }
                                }
                            }
                        }
                    }
                }";

            var variables = new { owner, name, since };
            var response = await ExecuteGraphQLQueryAsync<GitHubAnalyticsData>(query, variables);
            return response?.Repository;
        }

        public async Task<List<GitHubAnalyticsCommitNode>> GetUserContributionsAsync(string owner, string name, string username, int daysPeriod = 30)
        {
            var since = DateTime.UtcNow.AddDays(-daysPeriod).ToString("yyyy-MM-ddTHH:mm:ssZ");

            var query = @"
                query($owner: String!, $name: String!, $since: GitTimestamp!, $username: String!) {
                    repository(owner: $owner, name: $name) {
                        defaultBranchRef {
                            target {
                                ... on Commit {
                                    history(since: $since, author: { login: $username }) {
                                        totalCount
                                        nodes {
                                            id
                                            message
                                            committedDate
                                            author {
                                                name
                                                email
                                                user {
                                                    login
                                                }
                                            }
                                            additions
                                            deletions
                                            changedFiles
                                        }
                                    }
                                }
                            }
                        }
                    }
                }";

            var variables = new { owner, name, since, username };
            var response = await ExecuteGraphQLQueryAsync<GitHubAnalyticsData>(query, variables);
            
            if (response?.Repository?.DefaultBranchRef?.Target?.History?.Nodes != null)
            {
                return response.Repository.DefaultBranchRef.Target.History.Nodes;
            }

            return new List<GitHubAnalyticsCommitNode>();
        }

        public async Task<List<GitHubAnalyticsCommitNode>> GetCommitHistoryAsync(string owner, string name, int daysPeriod = 30)
        {
            var since = DateTime.UtcNow.AddDays(-daysPeriod).ToString("yyyy-MM-ddTHH:mm:ssZ");

            var query = @"
                query($owner: String!, $name: String!, $since: GitTimestamp!) {
                    repository(owner: $owner, name: $name) {
                        defaultBranchRef {
                            target {
                                ... on Commit {
                                    history(since: $since) {
                                        totalCount
                                        nodes {
                                            id
                                            message
                                            committedDate
                                            author {
                                                name
                                                email
                                                user {
                                                    login
                                                }
                                            }
                                            additions
                                            deletions
                                            changedFiles
                                        }
                                    }
                                }
                            }
                        }
                    }
                }";

            var variables = new { owner, name, since };
            var response = await ExecuteGraphQLQueryAsync<GitHubAnalyticsData>(query, variables);
            
            if (response?.Repository?.DefaultBranchRef?.Target?.History?.Nodes != null)
            {
                return response.Repository.DefaultBranchRef.Target.History.Nodes;
            }

            return new List<GitHubAnalyticsCommitNode>();
        }

        public async Task<List<GitHubIssueNode>> GetIssuesAsync(string owner, string name, int daysPeriod = 30)
        {
            var since = DateTime.UtcNow.AddDays(-daysPeriod).ToString("yyyy-MM-ddTHH:mm:ssZ");

            var query = @"
                query($owner: String!, $name: String!, $since: DateTime!) {
                    repository(owner: $owner, name: $name) {
                        issues(first: 100, orderBy: {field: CREATED_AT, direction: DESC}, filterBy: {since: $since}) {
                            nodes {
                                id
                                number
                                title
                                state
                                createdAt
                                closedAt
                                author {
                                    login
                                }
                                assignees(first: 10) {
                                    nodes {
                                        login
                                    }
                                }
                            }
                            totalCount
                        }
                    }
                }";

            var variables = new { owner, name, since };
            var response = await ExecuteGraphQLQueryAsync<dynamic>(query, variables);
            
            // Parse the response to extract issues
            // This is a simplified implementation - you might want to create a proper response model
            return new List<GitHubIssueNode>();
        }

        public async Task<List<GitHubPullRequestNode>> GetPullRequestsAsync(string owner, string name, int daysPeriod = 30)
        {
            var since = DateTime.UtcNow.AddDays(-daysPeriod).ToString("yyyy-MM-ddTHH:mm:ssZ");

            var query = @"
                query($owner: String!, $name: String!, $since: DateTime!) {
                    repository(owner: $owner, name: $name) {
                        pullRequests(first: 100, orderBy: {field: CREATED_AT, direction: DESC}, filterBy: {since: $since}) {
                            nodes {
                                id
                                number
                                title
                                state
                                createdAt
                                closedAt
                                mergedAt
                                author {
                                    login
                                }
                                reviews(first: 10) {
                                    nodes {
                                        id
                                        state
                                        createdAt
                                        author {
                                            login
                                        }
                                    }
                                    totalCount
                                }
                            }
                            totalCount
                        }
                    }
                }";

            var variables = new { owner, name, since };
            var response = await ExecuteGraphQLQueryAsync<dynamic>(query, variables);
            
            // Parse the response to extract pull requests
            return new List<GitHubPullRequestNode>();
        }

        public async Task<object> GetRepositoryStatsAsync(string owner, string name)
        {
            var query = @"
                query($owner: String!, $name: String!) {
                    repository(owner: $owner, name: $name) {
                        stargazerCount
                        forkCount
                        watchers {
                            totalCount
                        }
                        issues {
                            totalCount
                        }
                        pullRequests {
                            totalCount
                        }
                        refs {
                            totalCount
                        }
                        releases {
                            totalCount
                        }
                    }
                }";

            var variables = new { owner, name };
            var response = await ExecuteGraphQLQueryAsync<dynamic>(query, variables);
            return response ?? new object();
        }

        public async Task<bool> ValidateRepositoryAsync(string owner, string name)
        {
            try
            {
                var repository = await GetRepositoryAsync(owner, name);
                return repository != null && !repository.IsPrivate;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to validate repository {Owner}/{Name}", owner, name);
                return false;
            }
        }

        public async Task<(int Remaining, DateTime ResetAt)> GetRateLimitStatusAsync()
        {
            await _rateLimitSemaphore.WaitAsync();
            try
            {
                return (_remainingRequests, _rateLimitResetAt);
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }
        }

        public async Task<bool> HasRateLimitQuotaAsync(int requiredRequests = 1)
        {
            await _rateLimitSemaphore.WaitAsync();
            try
            {
                // Check if we need to reset the rate limit
                if (DateTime.UtcNow >= _rateLimitResetAt)
                {
                    _remainingRequests = _options.Value.MaxRequestsPerHour - _options.Value.RateLimitBuffer;
                    _rateLimitResetAt = DateTime.UtcNow.AddHours(1);
                }

                return _remainingRequests >= requiredRequests;
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }
        }

        private async Task<T?> ExecuteGraphQLQueryAsync<T>(string query, object variables) where T : class
        {
            if (!await HasRateLimitQuotaAsync(1))
            {
                throw new InvalidOperationException("GitHub API rate limit exceeded");
            }

            try
            {
                var requestBody = new
                {
                    query,
                    variables
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Create request with proper headers
                var request = new HttpRequestMessage(HttpMethod.Post, "");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetGitHubTokenAsync());
                request.Headers.Add("User-Agent", "GainIt-Platform");
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var graphQLResponse = JsonSerializer.Deserialize<GraphQLResponse<T>>(responseContent);
                    
                    // Update rate limit
                    await UpdateRateLimitAsync(response);
                    
                    if (graphQLResponse?.Errors != null && graphQLResponse.Errors.Any())
                    {
                        _logger.LogError("GraphQL errors: {Errors}", string.Join(", ", graphQLResponse.Errors.Select(e => e.Message)));
                        return default;
                    }

                    return graphQLResponse?.Data;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GitHub API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    return default;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GraphQL query");
                return default;
            }
            finally
            {
                // Decrement remaining requests
                await _rateLimitSemaphore.WaitAsync();
                try
                {
                    _remainingRequests = Math.Max(0, _remainingRequests - 1);
                }
                finally
                {
                    _rateLimitSemaphore.Release();
                }
            }
        }

        private string GetGitHubTokenAsync()
        {
            // This is a simplified implementation
            // In production, you should implement proper JWT token generation for GitHub Apps
            // See: https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/generating-a-jwt-for-a-github-app
            
            if (!string.IsNullOrEmpty(_options.Value.PrivateKeyContent))
            {
                // Generate JWT token using private key
                // This is a placeholder - implement actual JWT generation
                return "placeholder_jwt_token";
            }

            throw new InvalidOperationException("GitHub App private key not configured");
        }

        private async Task UpdateRateLimitAsync(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues) &&
                response.Headers.TryGetValues("X-RateLimit-Reset", out var resetValues))
            {
                if (int.TryParse(remainingValues.FirstOrDefault(), out var remaining) &&
                    long.TryParse(resetValues.FirstOrDefault(), out var resetTimestamp))
                {
                    await _rateLimitSemaphore.WaitAsync();
                    try
                    {
                        _remainingRequests = remaining;
                        _rateLimitResetAt = DateTimeOffset.FromUnixTimeSeconds(resetTimestamp).UtcDateTime;
                    }
                    finally
                    {
                        _rateLimitSemaphore.Release();
                    }
                }
            }
        }

        private class GraphQLResponse<T>
        {
            public T? Data { get; set; }
            public List<GraphQLError>? Errors { get; set; }
        }

        private class GraphQLError
        {
            public string Message { get; set; } = string.Empty;
        }
    }
}
