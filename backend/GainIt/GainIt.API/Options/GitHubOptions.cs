namespace GainIt.API.Options
{
    public class GitHubOptions
    {
        public const string SectionName = "GitHub";

        /// <summary>
        /// GitHub REST API endpoint
        /// </summary>
        public string RestApiEndpoint { get; set; } = "https://api.github.com";
        
        /// <summary>
        /// Maximum concurrent requests to GitHub API
        /// </summary>
        public int MaxConcurrentRequests { get; set; } = 5;
        
        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        public int RequestTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Rate limiting: maximum requests per hour (public API: 60)
        /// </summary>
        public int MaxRequestsPerHour { get; set; } = 60;
        
        /// <summary>
        /// Rate limit buffer to prevent hitting the limit
        /// </summary>
        public int RateLimitBuffer { get; set; } = 10;

        /// <summary>
        /// Default sync period in days
        /// </summary>
        public int DefaultSyncPeriodDays { get; set; } = 30;
        
        /// <summary>
        /// Maximum sync period in days
        /// </summary>
        public int MaxSyncPeriodDays { get; set; } = 365;
        
        /// <summary>
        /// How often to check for updates (in minutes)
        /// </summary>
        public int SyncIntervalMinutes { get; set; } = 60;
        
        /// <summary>
        /// Number of items to process in one batch
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Keep analytics data for this many days
        /// </summary>
        public int AnalyticsRetentionDays { get; set; } = 365;
        
        /// <summary>
        /// Keep contribution data for this many days
        /// </summary>
        public int ContributionRetentionDays { get; set; } = 365;
        
        /// <summary>
        /// Keep sync logs for this many days
        /// </summary>
        public int SyncLogRetentionDays { get; set; } = 90;

        /// <summary>
        /// Enable real-time sync (not supported in public API mode)
        /// </summary>
        public bool EnableRealTimeSync { get; set; } = false;
        
        /// <summary>
        /// Enable background sync
        /// </summary>
        public bool EnableBackgroundSync { get; set; } = true;
        
        /// <summary>
        /// Enable user analytics
        /// </summary>
        public bool EnableUserAnalytics { get; set; } = true;
        
        /// <summary>
        /// Enable project analytics
        /// </summary>
        public bool EnableProjectAnalytics { get; set; } = true;

        /// <summary>
        /// Enable detailed logging
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = true;
        
        /// <summary>
        /// Enable metrics collection
        /// </summary>
        public bool EnableMetricsCollection { get; set; } = true;
    }
}
