using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        private readonly ILogger<GitHubApiClient> _logger;
        private readonly SemaphoreSlim _rateLimitSemaphore;
        private DateTime _rateLimitResetAt = DateTime.UtcNow.AddHours(1);
        private int _remainingRequests = 5000; // Public API rate limit
        private readonly IConfiguration _configuration;

        public GitHubApiClient(
            HttpClient httpClient,
            ILogger<GitHubApiClient> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _rateLimitSemaphore = new SemaphoreSlim(1, 1);

            // Configure HTTP client for GitHub REST API
            _httpClient.BaseAddress = new Uri("https://api.github.com/");
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // Hardcoded timeout
            
            // Set GitHub API version and user agent
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GainIt-Platform");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            // Optional token to raise rate limits if provided via env var
            var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                        ?? _configuration["GitHub:Token"];
            if (!string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _logger.LogInformation("GitHub API client configured with token authentication (env GITHUB_TOKEN or config GitHub:Token)");
            }
            else
            {
                _logger.LogError("GitHub token is required but was not found (set env GITHUB_TOKEN or config GitHub:Token)");
                throw new InvalidOperationException("Missing GitHub token configuration");
            }
        }

        public async Task<GitHubRestApiRepository?> GetRepositoryAsync(string owner, string name)
        {
            try
            {
                _logger.LogInformation("Getting repository data for {Owner}/{Name} via REST API", owner, name);
                
                var response = await _httpClient.GetAsync($"repos/{owner}/{name}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to get repository {Owner}/{Name}. Status: {StatusCode}", owner, name, response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("REST API response for {Owner}/{Name}: {Length} characters", owner, name, json.Length);

                var repository = JsonSerializer.Deserialize<GitHubRestApiRepository>(json);
                return repository;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting repository data for {Owner}/{Name}", owner, name);
                return null;
            }
        }

        public async Task<GitHubAnalyticsRepository?> GetRepositoryAnalyticsAsync(string owner, string name, int daysPeriod = 30)
        {
            _logger.LogInformation("Getting repository analytics for {Owner}/{Name} via REST API", owner, name);
            
            try
            {
                if (!await HasRateLimitQuotaAsync(2)) // Commits endpoint + detailed commit
                {
                    throw new InvalidOperationException("GitHub API rate limit exceeded");
                }

            var since = DateTime.UtcNow.AddDays(-daysPeriod).ToString("yyyy-MM-ddTHH:mm:ssZ");
                var endpoint = $"repos/{owner}/{name}/commits?since={since}&per_page=100";
                
                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var commits = JsonSerializer.Deserialize<List<GitHubRestApiCommit>>(content);
                    
                    // Update rate limit
                    await UpdateRateLimitAsync(response);
                    
                    if (commits != null)
                    {
                        var analyticsRepo = new GitHubAnalyticsRepository
                        {
                            DefaultBranchRef = new GitHubAnalyticsRef
                            {
                                Target = new GitHubAnalyticsCommit
                                {
                                    History = new GitHubCommitHistory
                                    {
                                        TotalCount = commits.Count,
                                        Nodes = commits.Select(c => new GitHubAnalyticsCommitNode
                                        {
                                            Id = c.Sha,
                                            Message = c.Commit.Message,
                                            CommittedDate = c.Commit.Author.Date,
                                            Author = new GitHubCommitAuthor 
                                            { 
                                                Name = c.Commit.Author.Name, 
                                                Email = c.Commit.Author.Email 
                            },
                                            Additions = c.Stats?.Additions ?? 0,
                                            Deletions = c.Stats?.Deletions ?? 0,
                                            ChangedFiles = c.Files?.Count ?? 0
                                        }).ToList()
                                    }
                                }
                            }
                        };
                        
                        _logger.LogInformation("Analytics data retrieved for {Owner}/{Name}: {CommitCount} commits", 
                            owner, name, commits.Count);
                        
                        return analyticsRepo;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GitHub REST API error for analytics {Owner}/{Name}: {StatusCode} - {Content}", 
                        owner, name, response.StatusCode, errorContent);
                    
                    await UpdateRateLimitAsync(response);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting repository analytics for {Owner}/{Name}", owner, name);
                return null;
            }
            finally
            {
                await _rateLimitSemaphore.WaitAsync();
                try
                {
                    _remainingRequests = Math.Max(0, _remainingRequests - 2);
                }
                finally
                {
                    _rateLimitSemaphore.Release();
                }
            }
        }

        public async Task<List<GitHubAnalyticsCommitNode>> GetUserContributionsAsync(string owner, string name, string username, int daysPeriod = 30)
        {
            _logger.LogInformation("Getting user contributions for {Username} in {Owner}/{Name} via REST API", username, owner, name);
            
            try
            {
                if (!await HasRateLimitQuotaAsync(1))
                {
                    throw new InvalidOperationException("GitHub API rate limit exceeded");
                }

            var since = DateTime.UtcNow.AddDays(-daysPeriod).ToString("yyyy-MM-ddTHH:mm:ssZ");
                var endpoint = $"repos/{owner}/{name}/commits?author={username}&since={since}&per_page=100";
                
                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var commits = JsonSerializer.Deserialize<List<GitHubRestApiCommit>>(content);
                    
                    await UpdateRateLimitAsync(response);
                    
                    if (commits != null)
                    {
                        var contributions = commits.Select(c => new GitHubAnalyticsCommitNode
                        {
                            Id = c.Sha,
                            Message = c.Commit.Message,
                            CommittedDate = c.Commit.Author.Date,
                            Author = new GitHubCommitAuthor 
                            { 
                                Name = c.Commit.Author.Name, 
                                Email = c.Commit.Author.Email 
                            },
                            Additions = c.Stats?.Additions ?? 0,
                            Deletions = c.Stats?.Deletions ?? 0,
                            ChangedFiles = c.Files?.Count ?? 0
                        }).ToList();
                        
                        _logger.LogInformation("User contributions retrieved for {Username} in {Owner}/{Name}: {CommitCount} commits", 
                            username, owner, name, contributions.Count);
                        
                        return contributions;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GitHub REST API error for user contributions {Owner}/{Name}: {StatusCode} - {Content}", 
                        owner, name, response.StatusCode, errorContent);
                    
                    await UpdateRateLimitAsync(response);
                }
                
                return new List<GitHubAnalyticsCommitNode>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user contributions for {Username} in {Owner}/{Name}", username, owner, name);
                return new List<GitHubAnalyticsCommitNode>();
            }
            finally
            {
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

        public async Task<List<GitHubAnalyticsCommitNode>> GetUserContributionsForBranchAsync(string owner, string name, string username, string branch, int daysPeriod = 30)
        {
            _logger.LogInformation("Getting user contributions for {Username} in {Owner}/{Name} on branch {Branch} via REST API", username, owner, name, branch);
            
            try
            {
                if (!await HasRateLimitQuotaAsync(1))
                {
                    throw new InvalidOperationException("GitHub API rate limit exceeded");
                }

                var since = DateTime.UtcNow.AddDays(-daysPeriod).ToString("yyyy-MM-ddTHH:mm:ssZ");
                var endpoint = $"repos/{owner}/{name}/commits?sha={branch}&author={username}&since={since}&per_page=100";
                
                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var commits = JsonSerializer.Deserialize<List<GitHubRestApiCommit>>(content);
                    
                    await UpdateRateLimitAsync(response);
                    
                    if (commits != null)
                    {
                        var contributions = commits.Select(c => new GitHubAnalyticsCommitNode
                        {
                            Id = c.Sha,
                            Message = c.Commit.Message,
                            CommittedDate = c.Commit.Author.Date,
                            Author = new GitHubCommitAuthor 
                            { 
                                Name = c.Commit.Author.Name, 
                                Email = c.Commit.Author.Email 
                            },
                            Additions = c.Stats?.Additions ?? 0,
                            Deletions = c.Stats?.Deletions ?? 0,
                            ChangedFiles = c.Files?.Count ?? 0
                        }).ToList();
                        
                        _logger.LogInformation("User contributions retrieved for {Username} in {Owner}/{Name} on branch {Branch}: {CommitCount} commits", 
                            username, owner, name, branch, contributions.Count);
                        
                        return contributions;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GitHub REST API error for user contributions {Owner}/{Name} on branch {Branch}: {StatusCode} - {Content}", 
                        owner, name, branch, response.StatusCode, errorContent);
                    
                    await UpdateRateLimitAsync(response);
            }

            return new List<GitHubAnalyticsCommitNode>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user contributions for {Username} in {Owner}/{Name} on branch {Branch}", username, owner, name, branch);
            return new List<GitHubAnalyticsCommitNode>();
            }
            finally
            {
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

        public async Task<List<GitHubAnalyticsCommitNode>> GetCommitHistoryAsync(string owner, string name, int daysPeriod = 30)
        {
            _logger.LogInformation("Getting commit history for {Owner}/{Name} via REST API", owner, name);
            
            try
            {
                if (!await HasRateLimitQuotaAsync(1))
                {
                    throw new InvalidOperationException("GitHub API rate limit exceeded");
                }

            var since = DateTime.UtcNow.AddDays(-daysPeriod).ToString("yyyy-MM-ddTHH:mm:ssZ");
                var endpoint = $"repos/{owner}/{name}/commits?since={since}&per_page=100";
                
                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var commits = JsonSerializer.Deserialize<List<GitHubRestApiCommit>>(content);
                    
                    await UpdateRateLimitAsync(response);
                    
                    if (commits != null)
                    {
                        var commitHistory = commits.Select(c => new GitHubAnalyticsCommitNode
                        {
                            Id = c.Sha,
                            Message = c.Commit.Message,
                            CommittedDate = c.Commit.Author.Date,
                            Author = new GitHubCommitAuthor 
                            { 
                                Name = c.Commit.Author.Name, 
                                Email = c.Commit.Author.Email 
                            },
                            Additions = c.Stats?.Additions ?? 0,
                            Deletions = c.Stats?.Deletions ?? 0,
                            ChangedFiles = c.Files?.Count ?? 0
                        }).ToList();
                        
                        _logger.LogInformation("Commit history retrieved for {Owner}/{Name}: {CommitCount} commits", 
                            owner, name, commitHistory.Count);
                        
                        return commitHistory;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GitHub REST API error for commit history {Owner}/{Name}: {StatusCode} - {Content}", 
                        owner, name, response.StatusCode, errorContent);
                    
                    await UpdateRateLimitAsync(response);
                }

                return new List<GitHubAnalyticsCommitNode>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting commit history for {Owner}/{Name}", owner, name);
                return new List<GitHubAnalyticsCommitNode>();
            }
            finally
            {
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

        public async Task<GitHubRestApiCommit?> GetCommitDetailsAsync(string owner, string name, string sha)
        {
            _logger.LogInformation("Getting commit details for {Owner}/{Name}@{Sha} via REST API", owner, name, sha);
            try
            {
                if (!await HasRateLimitQuotaAsync(1))
                {
                    throw new InvalidOperationException("GitHub API rate limit exceeded");
                }

                var endpoint = $"repos/{owner}/{name}/commits/{sha}";
                var response = await _httpClient.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var commit = JsonSerializer.Deserialize<GitHubRestApiCommit>(content);
                    await UpdateRateLimitAsync(response);
                    return commit;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GitHub REST API error for commit details {Owner}/{Name}@{Sha}: {StatusCode} - {Content}", 
                        owner, name, sha, response.StatusCode, errorContent);
                    await UpdateRateLimitAsync(response);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting commit details for {Owner}/{Name}@{Sha}", owner, name, sha);
                return null;
            }
            finally
            {
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

        public async Task<List<GitHubIssueNode>> GetIssuesAsync(string owner, string name, int daysPeriod = 30)
        {
            _logger.LogInformation("Getting issues for {Owner}/{Name} via REST API", owner, name);
            
            try
            {
                if (!await HasRateLimitQuotaAsync(1))
                {
                    throw new InvalidOperationException("GitHub API rate limit exceeded");
                }
                // 10-minute cache for issues
                var cacheKey = $"issues:{owner}/{name}:{daysPeriod}";
                var (hit, cached) = InMemoryGitHubCache.TryGet<List<GitHubIssueNode>>(cacheKey);
                if (hit)
                {
                    return cached ?? new List<GitHubIssueNode>();
                }

                var since = DateTime.UtcNow.AddDays(-daysPeriod).ToString("yyyy-MM-ddTHH:mm:ssZ");
                var endpoint = $"repos/{owner}/{name}/issues?since={since}&state=all&per_page=100";
                
                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var issues = JsonSerializer.Deserialize<List<GitHubIssueNode>>(content);
                    
                    await UpdateRateLimitAsync(response);
                    
                    if (issues != null)
                    {
                        _logger.LogInformation("Issues retrieved for {Owner}/{Name}: {IssueCount} issues", 
                            owner, name, issues.Count);
                        InMemoryGitHubCache.Set(cacheKey, issues, TimeSpan.FromMinutes(10));
                        return issues;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GitHub REST API error for issues {Owner}/{Name}: {StatusCode} - {Content}", 
                        owner, name, response.StatusCode, errorContent);
                    
                    await UpdateRateLimitAsync(response);
            }

            return new List<GitHubIssueNode>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting issues for {Owner}/{Name}", owner, name);
                return new List<GitHubIssueNode>();
            }
            finally
            {
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

        public async Task<List<GitHubPullRequestNode>> GetPullRequestsAsync(string owner, string name, int daysPeriod = 30)
        {
            _logger.LogInformation("Getting pull requests for {Owner}/{Name} via REST API", owner, name);
            
            try
            {
                if (!await HasRateLimitQuotaAsync(1))
                {
                    throw new InvalidOperationException("GitHub API rate limit exceeded");
                }
                // 10-minute cache for PRs
                var cacheKey = $"prs:{owner}/{name}:{daysPeriod}";
                var (hit, cached) = InMemoryGitHubCache.TryGet<List<GitHubPullRequestNode>>(cacheKey);
                if (hit)
                {
                    return cached ?? new List<GitHubPullRequestNode>();
                }

                var since = DateTime.UtcNow.AddDays(-daysPeriod).ToString("yyyy-MM-ddTHH:mm:ssZ");
                var endpoint = $"repos/{owner}/{name}/pulls?state=all&per_page=100";
                
                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var pullRequests = JsonSerializer.Deserialize<List<GitHubPullRequestNode>>(content);
                    
                    await UpdateRateLimitAsync(response);
                    
                    if (pullRequests != null)
                    {
                        _logger.LogInformation("Pull requests retrieved for {Owner}/{Name}: {PRCount} PRs", 
                            owner, name, pullRequests.Count);
                        InMemoryGitHubCache.Set(cacheKey, pullRequests, TimeSpan.FromMinutes(10));
                        return pullRequests;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GitHub REST API error for pull requests {Owner}/{Name}: {StatusCode} - {Content}", 
                        owner, name, response.StatusCode, errorContent);
                    
                    await UpdateRateLimitAsync(response);
                }
                
                return new List<GitHubPullRequestNode>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pull requests for {Owner}/{Name}", owner, name);
                return new List<GitHubPullRequestNode>();
            }
            finally
        {
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

        public async Task<List<GitHubPullRequestReviewNode>> GetPullRequestReviewsAsync(string owner, string name, int pullNumber)
        {
            _logger.LogInformation("Getting pull request reviews for {Owner}/{Name} PR #{PR}", owner, name, pullNumber);
            try
            {
                if (!await HasRateLimitQuotaAsync(1))
                {
                    throw new InvalidOperationException("GitHub API rate limit exceeded");
                }

                var endpoint = $"repos/{owner}/{name}/pulls/{pullNumber}/reviews";
                var response = await _httpClient.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var reviews = JsonSerializer.Deserialize<List<GitHubPullRequestReviewNode>>(content);
                    await UpdateRateLimitAsync(response);
                    return reviews ?? new List<GitHubPullRequestReviewNode>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GitHub REST API error for PR reviews {Owner}/{Name} PR #{PR}: {StatusCode} - {Content}", 
                        owner, name, pullNumber, response.StatusCode, errorContent);
                    await UpdateRateLimitAsync(response);
                }

                return new List<GitHubPullRequestReviewNode>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PR reviews for {Owner}/{Name} PR #{PR}", owner, name, pullNumber);
                return new List<GitHubPullRequestReviewNode>();
            }
            finally
            {
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

        public async Task<GitHubRepositoryStats> GetRepositoryStatsAsync(string owner, string name)
        {
            _logger.LogInformation("Getting repository stats for {Owner}/{Name} via REST API", owner, name);
            
            try
            {
                if (!await HasRateLimitQuotaAsync(3)) // Main repo + languages + contributors
                {
                    throw new InvalidOperationException("GitHub API rate limit exceeded");
                }

                // Get main repository data
                var endpoint = $"repos/{owner}/{name}";
                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var repository = JsonSerializer.Deserialize<GitHubRestApiRepository>(content);
                    
                    await UpdateRateLimitAsync(response);
                    
                    if (repository != null)
                    {
                        // Get additional stats from separate endpoints
                        var (languages, contributors, branches) = await GetAdditionalStatsAsync(owner, name);
                        // Fetch PRs (cached for 10 minutes) and count open PRs
                        var prs = await GetPullRequestsAsync(owner, name, 30);
                        var openPrCount = prs?.Count(pr => string.Equals(pr.State, "open", StringComparison.OrdinalIgnoreCase)) ?? 0;
                        
                        var stats = new GitHubRepositoryStats
                        {
                            StargazerCount = repository.StargazersCount,
                            ForkCount = repository.ForksCount,
                            IssueCount = repository.OpenIssuesCount,
                            PullRequestCount = openPrCount,
                            BranchCount = branches,
                            ReleaseCount = 0 // Will be fetched separately if needed
                        };
                        
                        _logger.LogInformation("Repository stats retrieved for {Owner}/{Name}: {Stars} stars, {Forks} forks, {Languages} languages, {Contributors} contributors", 
                            owner, name, stats.StargazerCount, stats.ForkCount, languages?.Count ?? 0, contributors?.Count ?? 0);
                        
                        return stats;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GitHub REST API error for stats {Owner}/{Name}: {StatusCode} - {Content}", 
                        owner, name, response.StatusCode, errorContent);
                    
                    await UpdateRateLimitAsync(response);
            }

            return new GitHubRepositoryStats();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting repository stats for {Owner}/{Name}", owner, name);
                return new GitHubRepositoryStats();
            }
            finally
            {
                await _rateLimitSemaphore.WaitAsync();
                try
                {
                    _remainingRequests = Math.Max(0, _remainingRequests - 3);
                }
                finally
                {
                    _rateLimitSemaphore.Release();
                }
            }
        }

        private async Task<(Dictionary<string, int>? languages, List<GitHubRestApiContributor>? contributors, int branches)> GetAdditionalStatsAsync(string owner, string name)
        {
            var languages = new Dictionary<string, int>();
            var contributors = new List<GitHubRestApiContributor>();
            var branchCount = 0;

            try
            {
                // Get languages
                var languagesResponse = await _httpClient.GetAsync($"repos/{owner}/{name}/languages");
                if (languagesResponse.IsSuccessStatusCode)
                {
                    var languagesContent = await languagesResponse.Content.ReadAsStringAsync();
                    var languagesData = JsonSerializer.Deserialize<Dictionary<string, int>>(languagesContent);
                    if (languagesData != null)
                    {
                        languages = languagesData;
                    }
                }

                // Get contributors
                var contributorsResponse = await _httpClient.GetAsync($"repos/{owner}/{name}/contributors?per_page=100");
                if (contributorsResponse.IsSuccessStatusCode)
                {
                    var contributorsContent = await contributorsResponse.Content.ReadAsStringAsync();
                    var contributorsData = JsonSerializer.Deserialize<List<GitHubRestApiContributor>>(contributorsContent);
                    if (contributorsData != null)
                    {
                        contributors = contributorsData;
                    }
                }

                // Get branches count
                var branchesResponse = await _httpClient.GetAsync($"repos/{owner}/{name}/branches?per_page=100");
                if (branchesResponse.IsSuccessStatusCode)
                {
                    var branchesContent = await branchesResponse.Content.ReadAsStringAsync();
                    var branchesData = JsonSerializer.Deserialize<List<object>>(branchesContent);
                    if (branchesData != null)
                    {
                        branchCount = branchesData.Count;
                    }
                }

                _logger.LogInformation("Additional stats retrieved for {Owner}/{Name}: {LanguageCount} languages, {ContributorCount} contributors, {BranchCount} branches", 
                    owner, name, languages.Count, contributors.Count, branchCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting additional stats for {Owner}/{Name}", owner, name);
            }

            return (languages, contributors, branchCount);
        }

        public async Task<Dictionary<string, int>> GetRepositoryLanguagesAsync(string owner, string name)
        {
            _logger.LogInformation("Getting repository languages for {Owner}/{Name} via REST API", owner, name);
            
            try
            {
                if (!await HasRateLimitQuotaAsync(1))
                {
                    throw new InvalidOperationException("GitHub API rate limit exceeded");
                }

                var endpoint = $"repos/{owner}/{name}/languages";
                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var languages = JsonSerializer.Deserialize<Dictionary<string, int>>(content);
                    
                    await UpdateRateLimitAsync(response);
                    
                    if (languages != null)
                    {
                        _logger.LogInformation("Languages retrieved for {Owner}/{Name}: {LanguageCount} languages", 
                            owner, name, languages.Count);
                        
                        return languages;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GitHub REST API error for languages {Owner}/{Name}: {StatusCode} - {Content}", 
                        owner, name, response.StatusCode, errorContent);
                    
                    await UpdateRateLimitAsync(response);
                }
                
                return new Dictionary<string, int>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting repository languages for {Owner}/{Name}", owner, name);
                return new Dictionary<string, int>();
            }
            finally
            {
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

        public async Task<List<GitHubRestApiContributor>> GetRepositoryContributorsAsync(string owner, string name)
        {
            _logger.LogInformation("Getting repository contributors for {Owner}/{Name} via REST API", owner, name);
            
            try
            {
                if (!await HasRateLimitQuotaAsync(1))
                {
                    throw new InvalidOperationException("GitHub API rate limit exceeded");
                }

                var endpoint = $"repos/{owner}/{name}/contributors?per_page=100";
                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var contributors = JsonSerializer.Deserialize<List<GitHubRestApiContributor>>(content);
                    
                    await UpdateRateLimitAsync(response);
                    
                    if (contributors != null)
                    {
                        _logger.LogInformation("Contributors retrieved for {Owner}/{Name}: {ContributorCount} contributors", 
                            owner, name, contributors.Count);
                        
                        return contributors;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GitHub REST API error for contributors {Owner}/{Name}: {StatusCode} - {Content}", 
                        owner, name, response.StatusCode, errorContent);
                    
                    await UpdateRateLimitAsync(response);
                }
                
                return new List<GitHubRestApiContributor>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting repository contributors for {Owner}/{Name}", owner, name);
                return new List<GitHubRestApiContributor>();
            }
            finally
                {
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

        public async Task<List<string>> GetRepositoryBranchesAsync(string owner, string name)
        {
            _logger.LogInformation("Getting repository branches for {Owner}/{Name} via REST API", owner, name);
            
            try
            {
                if (!await HasRateLimitQuotaAsync(1))
                {
                    throw new InvalidOperationException("GitHub API rate limit exceeded");
                }

                var endpoint = $"repos/{owner}/{name}/branches?per_page=100";
                var response = await _httpClient.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var branchesData = JsonSerializer.Deserialize<List<JsonElement>>(content);
                    
                    await UpdateRateLimitAsync(response);
                    
                    if (branchesData != null)
                    {
                        var branches = branchesData
                            .Where(b => b.TryGetProperty("name", out var nameProp))
                            .Select(b => b.GetProperty("name").GetString() ?? string.Empty)
                            .Where(name => !string.IsNullOrEmpty(name))
                            .ToList();
                        
                        _logger.LogInformation("Branches retrieved for {Owner}/{Name}: {BranchCount} branches", 
                            owner, name, branches.Count);
                        
                        return branches;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("GitHub REST API error for branches {Owner}/{Name}: {StatusCode} - {Content}", 
                        owner, name, response.StatusCode, errorContent);
                    
                    await UpdateRateLimitAsync(response);
                }
                
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting repository branches for {Owner}/{Name}", owner, name);
                return new List<string>();
            }
            finally
            {
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

        public async Task<(bool IsValid, GitHubRestApiRepository? Repository, bool? IsPrivate)> ValidateRepositoryAsync(string owner, string name)
        {
            try
            {
                var repository = await GetRepositoryAsync(owner, name);
                
                if (repository == null)
                {
                    _logger.LogWarning("Repository {Owner}/{Name} not found or inaccessible", owner, name);
                    return (false, null, null);
                }

                var isValid = !string.IsNullOrEmpty(repository.Name) && !string.IsNullOrEmpty(repository.FullName);
                var isPrivate = repository.Private;

                _logger.LogInformation("Repository validation result: {IsValid}, Repository: {Repository}, IsPrivate: {IsPrivate}", 
                    isValid, repository?.FullName ?? "null", isPrivate);

                return (isValid, repository, isPrivate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating repository {Owner}/{Name}", owner, name);
                return (false, null, null);
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
                    _remainingRequests = 60; // Public API rate limit
                    _rateLimitResetAt = DateTime.UtcNow.AddHours(1);
                }

                return _remainingRequests >= requiredRequests;
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }
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
    }
}
