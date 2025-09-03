using Azure.AI.OpenAI;
using GainIt.API.Data;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;
using GainIt.API.Services.Users.Interfaces;
using GainIt.API.Services.GitHub.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GainIt.API.Options;
using OpenAI.Chat;
using System.Text;

namespace GainIt.API.Services.Users.Implementations
{
    public class UserSummaryService : IUserSummaryService
    {
        private readonly GainItDbContext r_DbContext;
        private readonly ILogger<UserSummaryService> r_logger;
        private readonly IMemoryCache r_cache;
        private readonly AzureOpenAIClient r_openAIClient;
        private readonly ChatClient r_chatClient;
        private readonly IGitHubAnalyticsService r_gitHubAnalyticsService;
        private readonly IGitHubService r_gitHubService;

        public UserSummaryService(
            GainItDbContext dbContext,
            ILogger<UserSummaryService> logger,
            IMemoryCache cache,
            AzureOpenAIClient openAIClient,
            IOptions<OpenAIOptions> openAIOptionsAccessor,
            IGitHubAnalyticsService gitHubAnalyticsService,
            IGitHubService gitHubService)
        {
            r_DbContext = dbContext;
            r_logger = logger;
            r_cache = cache;
            r_openAIClient = openAIClient;
            r_gitHubAnalyticsService = gitHubAnalyticsService;
            r_gitHubService = gitHubService;
            // Use the same configuration pattern as ProjectMatchingService
            r_chatClient = openAIClient.GetChatClient(openAIOptionsAccessor.Value.ChatDeploymentName);
        }

        public async Task<string> GetUserSummaryAsync(Guid userId)
        {
            var cacheKey = $"user-summary:{userId}";
            if (r_cache.TryGetValue(cacheKey, out string cached))
            {
                r_logger.LogDebug("User summary cache hit for UserId={UserId}", userId);
                return cached ?? string.Empty;
            }

            r_logger.LogInformation("Generating fresh user summary for UserId={UserId}", userId);

            // 1) Load base user and platform activity (all-time)
            var user = await r_DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User {userId} not found");
            }

            // Project participation/ownership
            var participatedProjectsCount = await r_DbContext.Projects
                .CountAsync(p => p.ProjectMembers.Any(pm => pm.UserId == userId));

            var ownedProjectsCount = await r_DbContext.Projects
                .CountAsync(p => p.OwningOrganizationUserId == userId);

            var activeProjectsCount = await r_DbContext.Projects
                .CountAsync(p => p.ProjectMembers.Any(pm => pm.UserId == userId) && p.ProjectStatus != Models.Enums.Projects.eProjectStatus.Completed);

            var completedProjectsCount = await r_DbContext.Projects
                .CountAsync(p => p.ProjectMembers.Any(pm => pm.UserId == userId) && p.ProjectStatus == Models.Enums.Projects.eProjectStatus.Completed);

            // Tasks (assigned/created where available)
            var tasksAssigned = await r_DbContext.ProjectTasks
                .CountAsync(t => t.AssignedUserId == userId);

            var tasksCompleted = await r_DbContext.ProjectTasks
                .CountAsync(t => t.AssignedUserId == userId && t.Status == Models.Enums.Tasks.eTaskStatus.Done);

            // Achievements
            var achievementsCount = await r_DbContext.UserAchievements
                .CountAsync(a => a.UserId == userId);

            // Aggregated languages/technologies from participated projects
            var projTechData = await r_DbContext.Projects
                .Where(p => p.ProjectMembers.Any(pm => pm.UserId == userId))
                .Select(p => new { p.ProjectId, p.ProgrammingLanguages, p.Technologies, p.RepositoryLink })
                .ToListAsync();

            var languages = projTechData
                .Where(x => x.ProgrammingLanguages != null)
                .SelectMany(x => x.ProgrammingLanguages!)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            var technologies = projTechData
                .Where(x => x.Technologies != null)
                .SelectMany(x => x.Technologies!)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            //added also forum likes/posts counts
            var forumLikesCount = await r_DbContext.ForumPostLikes
                .CountAsync(l => l.UserId == userId);
            var forumPostsCount = await r_DbContext.ForumPosts
                .CountAsync(p => p.AuthorId == userId);

            var forumRepliesCount = await r_DbContext.ForumReplies
                .CountAsync(r => r.AuthorId == userId);
            
                

            // 2) GitHub summary fact (fresh: use GitHubService.GetUserActivitySummaryAsync per project with repo)
            string githubFactLine = await getGitHubSummaryFactAsync(user, projTechData.Select(p => p.ProjectId).ToList());

            // Calculate activity level indicators
            var totalPlatformActivity = participatedProjectsCount + tasksAssigned + achievementsCount + forumPostsCount + forumRepliesCount;
            var isHighPlatformActivity = totalPlatformActivity >= 10; // Threshold for "high activity"
            var completionRate = tasksAssigned > 0 ? (double)tasksCompleted / tasksAssigned * 100 : 0;
            var hasLeadershipIndicators = ownedProjectsCount > 0 || forumPostsCount > 5; // Forum posts suggest community engagement

            r_logger.LogDebug("User summary facts collected for UserId={UserId}: Projects={Projects}, Tasks={Tasks}, Achievements={Achievements}, Languages={Languages}, Technologies={Technologies}, GitHubFact={GitHubFact}, ActivityLevel={ActivityLevel}", 
                userId, participatedProjectsCount, tasksAssigned, achievementsCount, languages.Count, technologies.Count, !string.IsNullOrWhiteSpace(githubFactLine), isHighPlatformActivity ? "HIGH" : "LOW");

            var facts = new StringBuilder();
            facts.AppendLine($"Name: {user.FullName}");
            facts.AppendLine($"Platform Activity Level: {(isHighPlatformActivity ? "HIGH" : "LOW")} (Total activities: {totalPlatformActivity})");
            facts.AppendLine($"Participated projects: {participatedProjectsCount}");
            facts.AppendLine($"Owned projects: {ownedProjectsCount}");
            facts.AppendLine($"Active projects: {activeProjectsCount}");
            facts.AppendLine($"Completed projects: {completedProjectsCount}");
            facts.AppendLine($"Tasks assigned: {tasksAssigned}");
            facts.AppendLine($"Tasks completed: {tasksCompleted}");
            facts.AppendLine($"Task completion rate: {completionRate:F1}%");
            facts.AppendLine($"Achievements earned: {achievementsCount}");
            facts.AppendLine($"Forum engagement: {forumPostsCount} posts, {forumRepliesCount} replies, {forumLikesCount} likes");
            facts.AppendLine($"Leadership indicators: {(hasLeadershipIndicators ? "YES" : "NO")} (Project ownership or high forum engagement)");
            if (languages.Any())
            {
                var sample = string.Join(", ", languages.Take(10));
                var suffix = languages.Count > 10 ? $" (+{languages.Count - 10})" : string.Empty;
                facts.AppendLine($"Languages used: {sample}{suffix}");
            }
            if (technologies.Any())
            {
                var sample = string.Join(", ", technologies.Take(10));
                var suffix = technologies.Count > 10 ? $" (+{technologies.Count - 10})" : string.Empty;
                facts.AppendLine($"Technologies used: {sample}{suffix}");
            }
            if (!string.IsNullOrWhiteSpace(githubFactLine))
            {
                facts.AppendLine(githubFactLine);
            }

            var bullets = await generateRecruiterSummaryAsync(facts.ToString());

            // Cache 24h
            r_cache.Set(cacheKey, bullets, TimeSpan.FromHours(24));
            r_logger.LogInformation("User summary generated and cached for UserId={UserId}, SummaryLength={Length}", userId, bullets.Length);
            return bullets;
        }

        private async Task<string> generateRecruiterSummaryAsync(string facts)
        {
            // Guard against empty/whitespace facts
            var safeFacts = string.IsNullOrWhiteSpace(facts) ? "No activity facts available." : facts;

            var messages = new ChatMessage[]
            {
                new SystemChatMessage(
                    "You are an experienced recruiter with 20+ years in the industry writing a compelling candidate summary. " +
                    "Use ONLY the provided facts; do NOT infer or assume. If a fact is absent, omit it.\n\n" +
                    "STRUCTURE (8-12 bullets total):\n" +
                    "• Platform Impact (2-3 bullets): Project leadership, team collaboration, task completion\n" +
                    "• Technical Excellence (2-3 bullets): Languages, technologies, GitHub contributions\n" +
                    "• Professional Growth (2-3 bullets): Achievements, forum engagement, skill development\n" +
                    "• Leadership & Initiative (1-2 bullets): Project ownership, mentoring, community involvement\n" +
                    "• Overall Assessment (1 bullet): Key differentiator or standout quality\n\n" +
                    "PRIORITIZATION RULES:\n" +
                    "- If HIGH platform activity: Emphasize leadership, project management, team collaboration\n" +
                    "- If LOW platform activity but HIGH GitHub: Emphasize technical skills, code contributions\n" +
                    "- Blend platform and GitHub data when both available\n" +
                    "- Include specific metrics and timeframes when they add value\n" +
                    "- Use narrative style with action verbs and quantify impact\n" +
                    "- 15-25 words per bullet, professional tone"),
                new UserChatMessage(
                    $"Candidate activity data (all-time platform engagement):\n\n{safeFacts}\n\n" +
                    "Generate a compelling 8-12 bullet summary following the structure above. " +
                    "Focus on what makes this candidate stand out to recruiters and hiring managers.")
            };

            var options = new ChatCompletionOptions { Temperature = 0.3f };
            ChatCompletion completion = await r_chatClient.CompleteChatAsync(messages, options);
            var text = completion.Content[0].Text.Trim();
            return text;
        }

        private async Task<string> getGitHubSummaryFactAsync(User user, List<Guid> userProjectIds)
        {
            var userId = user.UserId;
            var cacheKey = $"user-github-fact:{userId}";
            if (r_cache.TryGetValue(cacheKey, out string cached))
            {
                r_logger.LogDebug("GitHub fact cache hit for UserId={UserId}", userId);
                return cached;
            }

            r_logger.LogDebug("Generating fresh GitHub fact for UserId={UserId}, ProjectCount={ProjectCount}", userId, userProjectIds.Count);

            // Only consider projects with a repo link
            var repoProjects = await r_DbContext.Projects
                .Where(p => userProjectIds.Contains(p.ProjectId) && !string.IsNullOrWhiteSpace(p.RepositoryLink))
                .Select(p => p.ProjectId)
                .ToListAsync();

            if (string.IsNullOrWhiteSpace(user.GitHubUsername) && !repoProjects.Any())
            {
                r_cache.Set(cacheKey, string.Empty, TimeSpan.FromHours(24));
                return string.Empty;
            }

            // Build fresh impact lines per repo using GitHubService (up-to-date analytics + contributions)
            var perRepo = new List<string>();
            foreach (var pid in repoProjects.Take(3)) // limit to first 3 for brevity
            {
                try
                {
                    var summary = await r_gitHubService.GetUserActivitySummaryAsync(pid, userId, 30);
                    if (!string.IsNullOrWhiteSpace(summary))
                    {
                        perRepo.Add(summary.Trim());
                    }
                }
                catch (Exception ex)
                {
                    r_logger.LogWarning(ex, "Failed to get GitHub activity summary for ProjectId={ProjectId}, UserId={UserId}", pid, userId);
                }
            }

            string fact;
            if (perRepo.Any())
            {
                var explanation = string.Join(" | ", perRepo);
                var oneLine = explanation.Replace("\r", " ").Replace("\n", "; ").Trim();
                if (oneLine.Length > 280) oneLine = oneLine.Substring(0, 277) + "...";
                fact = $"GitHub impact: {oneLine}";
            }
            else
            {
                // Fallback simple facts
                int linkedRepos = repoProjects.Count;
                fact = !string.IsNullOrWhiteSpace(user.GitHubUsername) || linkedRepos > 0
                    ? $"GitHub: username={(string.IsNullOrWhiteSpace(user.GitHubUsername) ? "none" : user.GitHubUsername)}, linkedRepos={linkedRepos}"
                    : string.Empty;
            }

            r_cache.Set(cacheKey, fact, TimeSpan.FromHours(24));
            r_logger.LogDebug("GitHub fact generated and cached for UserId={UserId}, FactLength={Length}", userId, fact.Length);
            return fact;
        }
    }
}


