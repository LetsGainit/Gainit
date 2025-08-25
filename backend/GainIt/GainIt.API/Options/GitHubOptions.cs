namespace GainIt.API.Options
{
    public class GitHubOptions
    {
        public const string SectionName = "GitHub";

        // GitHub App Configuration
        public string AppId { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string AppDescription { get; set; } = string.Empty;
        public string AppUrl { get; set; } = string.Empty;
        public string AppCallbackUrl { get; set; } = string.Empty;

        // GitHub App Installation
        public string InstallationId { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;

        // Private Key (should be stored securely, not in appsettings)
        public string PrivateKeyPath { get; set; } = string.Empty;
        public string PrivateKeyContent { get; set; } = string.Empty;

        // API Configuration
        public string GraphQLEndpoint { get; set; } = "https://api.github.com/graphql";
        public string RestApiEndpoint { get; set; } = "https://api.github.com";
        public int MaxConcurrentRequests { get; set; } = 5;
        public int RequestTimeoutSeconds { get; set; } = 30;

        // Rate Limiting
        public int MaxRequestsPerHour { get; set; } = 5000;
        public int RateLimitBuffer { get; set; } = 100; // Keep 100 requests as buffer

        // Sync Configuration
        public int DefaultSyncPeriodDays { get; set; } = 30;
        public int MaxSyncPeriodDays { get; set; } = 365;
        public int SyncIntervalMinutes { get; set; } = 60; // How often to check for updates
        public int BatchSize { get; set; } = 100; // Number of items to process in one batch

        // Data Retention
        public int AnalyticsRetentionDays { get; set; } = 365; // Keep 1 year of data
        public int ContributionRetentionDays { get; set; } = 365;
        public int SyncLogRetentionDays { get; set; } = 90;

        // Feature Flags
        public bool EnableRealTimeSync { get; set; } = false;
        public bool EnableBackgroundSync { get; set; } = true;
        public bool EnableUserAnalytics { get; set; } = true;
        public bool EnableProjectAnalytics { get; set; } = true;

        // Webhook Configuration (for real-time updates)
        public string WebhookSecret { get; set; } = string.Empty;
        public bool EnableWebhooks { get; set; } = false;

        // Logging and Monitoring
        public bool EnableDetailedLogging { get; set; } = false;
        public bool EnableMetricsCollection { get; set; } = true;
    }
}
