# GitHub Integration for GainIt Platform

## Overview

The GitHub integration feature allows projects on the GainIt platform to connect with GitHub repositories and automatically track analytics about user involvement, including commits, issues, pull requests, and more. This integration provides both project-level analytics and user-level contribution tracking.

**Design**: Users provide their GitHub username during registration. The system uses GitHub App authentication to access public repository data and maps contributions using the stored usernames.

## Features

### 🚀 Core Functionality
- **Repository Linking**: Connect projects to GitHub repositories
- **Real-time Analytics**: Track repository health and activity metrics
- **User Contributions**: Monitor individual user contributions and patterns
- **ChatGPT Integration**: Generate contextual summaries for AI assistance
- **Automatic Sync**: Background synchronization of GitHub data
- **Rate Limit Management**: Intelligent handling of GitHub API limits
- **Simple User Setup**: GitHub username input during registration

### 📊 Analytics Metrics

#### Project-Level Analytics
- **Repository Health**: Stars, forks, watchers, activity patterns
- **Code Activity**: Total commits, lines changed, file modifications
- **Issue Management**: Open/closed issues, average resolution time
- **Pull Request Metrics**: Open/merged/closed PRs, review statistics
- **Language Distribution**: Programming languages used in the repository
- **Time-based Patterns**: Weekly and monthly activity trends

#### User-Level Analytics
- **Commit Activity**: Total commits, lines added/removed, active days
- **Issue Participation**: Issues created, assigned, commented on
- **Pull Request Activity**: PRs created, reviewed, approved
- **Code Quality**: Average commit size, files modified, languages used
- **Collaboration**: Interactions with other contributors
- **Activity Patterns**: Time-based contribution patterns

## Architecture

### Database Models

#### GitHubRepository
- Links projects to GitHub repositories
- Stores repository metadata and statistics
- Tracks synchronization status

#### GitHubAnalytics
- Aggregated project-level analytics
- Time-based metrics and trends
- Repository health scoring

#### GitHubContribution
- Individual user contribution data
- Activity patterns and metrics
- Contribution quality scoring

#### GitHubSyncLog
- Synchronization history and status
- Error tracking and rate limit information
- Performance monitoring

#### User Model
- **GitHubUsername Field**: Stores GitHub username for contribution mapping
- **Purpose**: Direct mapping without OAuth complexity
- **Benefits**: Simple storage, easy updates, reliable mapping

### Service Layer

#### IGitHubService
- Main service interface for GitHub operations
- Repository management and analytics retrieval
- ChatGPT context generation
- User contribution mapping using stored GitHubUsername

#### IGitHubApiClient
- GitHub GraphQL API integration
- Rate limit management
- Error handling and retry logic
- GitHub App authentication

#### IGitHubAnalyticsService
- Analytics processing and aggregation
- Score calculation algorithms
- Data export and cleanup

## Setup Instructions

### 1. GitHub App Configuration

#### Create a GitHub App
1. Go to [GitHub Developer Settings](https://github.com/settings/apps)
2. Click "New GitHub App"
3. Configure the app with the following settings:
   - **App name**: `GainIt GitHub Integration`
   - **Description**: `GitHub integration for GainIt platform`
   - **Homepage URL**: Your platform URL
   - **Callback URL**: `https://your-domain.com/github/callback`
   - **Webhook**: Enable if you want real-time updates
   - **Permissions**:
     - Repository: `Read` (for public repositories)
     - Issues: `Read`
     - Pull requests: `Read`
     - Contents: `Read`
     - Metadata: `Read`

#### Install the App
1. After creating the app, install it to your organization
2. Note the Installation ID
3. Generate and download the private key

### 2. Configuration Setup

#### Development Environment
Create `appsettings.GitHub.Development.json` (add to .gitignore):
```json
{
  "GitHub": {
    "AppId": "your-actual-github-app-id",
    "PrivateKeyContent": "-----BEGIN RSA PRIVATE KEY-----\nYour actual private key here\n-----END RSA PRIVATE KEY-----",
    "WebhookSecret": "your-actual-webhook-secret",
    "InstallationId": "your-actual-installation-id"
  }
}
```

#### Production Environment
Set these environment variables in Azure Web App:
- `GitHub__AppId`
- `GitHub__PrivateKeyContent`
- `GitHub__WebhookSecret`
- `GitHub__InstallationId`

#### Template Configuration
The `appsettings.GitHub.json` file is safe to commit and contains:
- App metadata and descriptions
- API endpoints and timeouts
- Rate limiting configuration
- Sync intervals and retention policies

### 3. Database Migration

Run the Entity Framework migration to add the GitHubUsername field:
```bash
dotnet ef migrations add AddGitHubUsernameField
dotnet ef database update
```

### 4. User Registration Flow

#### Frontend Integration
Add GitHub username field to user registration forms:
```html
<div class="form-group">
  <label for="githubUsername">GitHub Username (Optional)</label>
  <input type="text" id="githubUsername" name="githubUsername" 
         placeholder="e.g., johndoe" class="form-control">
  <small class="form-text text-muted">
    We'll use this to track your contributions to project repositories
  </small>
</div>
```

#### Backend Processing
The `ExternalUserDto` includes `GitHubUsername`:
```csharp
public class ExternalUserDto
{
    public string ExternalId { get; init; } = default!;
    public string? Email { get; init; }
    public string? FullName { get; init; }
    public string? IdentityProvider { get; init; }
    public string? Country { get; init; }
    public string? GitHubUsername { get; init; }  // GitHub username field
}
```

## API Endpoints

### Repository Management

#### Link Repository
```http
POST /api/github/projects/{projectId}/link
Content-Type: application/json

{
  "repositoryUrl": "https://github.com/username/repository"
}
```

#### Unlink Repository
```http
DELETE /api/github/projects/{projectId}/unlink
```

#### Validate Repository URL
```http
POST /api/github/validate-url
Content-Type: application/json

{
  "repositoryUrl": "https://github.com/username/repository"
}
```

### Analytics

#### Get Project Analytics
```http
GET /api/github/projects/{projectId}/analytics?daysPeriod=30
```

#### Get User Contributions
```http
GET /api/github/projects/{projectId}/contributions
```

#### Get Specific User Contribution
```http
GET /api/github/projects/{projectId}/contributions/{userId}
```

#### Get Repository Statistics
```http
GET /api/github/projects/{projectId}/stats
```

### ChatGPT Integration

#### Get Project Activity Summary
```http
GET /api/github/projects/{projectId}/activity-summary?daysPeriod=30
```

#### Get User Activity Summary
```http
GET /api/github/projects/{projectId}/users/{userId}/activity-summary
```

### Synchronization

#### Sync Project Data
```http
POST /api/github/projects/{projectId}/sync?syncType=all
```

#### Get Sync Status
```http
GET /api/github/projects/{projectId}/sync-status
```

## Usage Examples

### Frontend Integration

#### Link a Repository
```javascript
const linkRepository = async (projectId, repositoryUrl) => {
  const response = await fetch(`/api/github/projects/${projectId}/link`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({ repositoryUrl })
  });
  
  if (response.ok) {
    const result = await response.json();
    console.log('Repository linked:', result.repository);
  }
};
```

#### Display Analytics
```javascript
const getProjectAnalytics = async (projectId) => {
  const response = await fetch(`/api/github/projects/${projectId}/analytics`);
  if (response.ok) {
    const analytics = await response.json();
    
    // Display repository health score
    const healthScore = analytics.analytics.healthScore;
    
    // Show activity trends
    const weeklyCommits = analytics.analytics.weeklyCommits;
    
    // Display top contributors
    const topContributors = analytics.contributions
      .sort((a, b) => b.totalCommits - a.totalCommits)
      .slice(0, 5);
  }
};
```

### ChatGPT Integration

#### Get Context for User
```javascript
const getUserContext = async (projectId, userId) => {
  const response = await fetch(
    `/api/github/projects/${projectId}/users/${userId}/activity-summary`
  );
  
  if (response.ok) {
    const summary = await response.json();
    
    // Use this summary as context for ChatGPT
    const chatContext = `
      User Activity Summary:
      ${summary.summary}
      
      Based on this information, provide personalized recommendations...
    `;
  }
};
```

## Rate Limiting

The integration includes intelligent rate limit management:

- **GitHub API Limit**: 5,000 requests per hour for authenticated requests
- **Buffer Management**: Keeps 100 requests as buffer for critical operations
- **Automatic Throttling**: Pauses operations when approaching limits
- **Retry Logic**: Automatic retry with exponential backoff

## Data Retention

- **Analytics Data**: 1 year (configurable)
- **Contribution Data**: 1 year (configurable)
- **Sync Logs**: 90 days (configurable)
- **Automatic Cleanup**: Background service removes old data

## Security Considerations

- **GitHub Apps**: Uses GitHub Apps for secure, scoped access
- **App-Level Authentication**: Single authentication for all public repositories
- **No User Tokens**: Users only provide GitHub username, no OAuth tokens stored
- **Public Repositories Only**: Only accesses public repository data
- **Rate Limiting**: Prevents API abuse and quota exhaustion

## User Experience Benefits

### ✅ Simplified Setup
- **No OAuth Flow**: Users don't need to authorize GitHub app
- **Instant Setup**: GitHub username can be provided immediately
- **No Redirects**: No callback URLs or webhook setup required
- **Privacy Friendly**: Only public repository data is accessed

### ✅ Reliable Operation
- **No Token Expiration**: GitHub App tokens don't expire
- **No Password Changes**: Won't break if users change GitHub passwords
- **Consistent Access**: Same level of access for all users
- **Better Rate Limits**: GitHub Apps get higher rate limits

### ✅ Easy Maintenance
- **No Token Refresh**: No need to handle OAuth token refresh
- **No User Revocation**: Users can't accidentally revoke access
- **Centralized Management**: Single GitHub App manages all access
- **Simplified Troubleshooting**: Fewer moving parts to debug

## Troubleshooting

### Common Issues

#### Repository Not Found
- Ensure the repository URL is correct
- Verify the repository is public
- Check if the GitHub App has proper permissions

#### Rate Limit Exceeded
- The system automatically handles rate limiting
- Check sync logs for rate limit information
- Consider adjusting sync intervals

#### User Contributions Not Showing
- Verify the user has provided their GitHub username
- Check if the username matches their actual GitHub account
- Ensure the user is a member of the project

#### Authentication Errors
- Verify GitHub App configuration
- Check private key format and content
- Ensure Installation ID is correct

### Debug Information

#### Check Sync Status
```http
GET /api/github/projects/{projectId}/sync-status
```

#### View Sync Logs
Check the `GitHubSyncLogs` table for detailed sync information.

#### Monitor Rate Limits
The system logs rate limit information in sync logs and application logs.

## Future Enhancements

### Phase 2 Features
- **Background Sync Service**: Automatic periodic synchronization
- **Webhook Integration**: Real-time updates from GitHub
- **Advanced Analytics**: Machine learning insights
- **Private Repository Support**: With user consent

### Phase 3 Features
- **Achievement System**: GitHub-based badges and rewards
- **Team Analytics**: Group-level contribution analysis
- **Performance Metrics**: Code quality and efficiency scoring
- **Integration APIs**: Third-party tool integration

## Conclusion

The GitHub integration provides a powerful yet simple way to track project analytics and user contributions. By using GitHub Apps for authentication and storing only GitHub usernames, the system eliminates OAuth complexity while maintaining all the analytics capabilities.

The integration is designed to be:
- **User-Friendly**: Simple username input during registration
- **Secure**: No OAuth tokens stored, only public data accessed
- **Reliable**: App-level authentication with no expiration
- **Scalable**: Efficient rate limiting and data management
- **Maintainable**: Clean architecture and comprehensive documentation

This approach provides the best balance of functionality, security, and user experience for tracking GitHub contributions on the GainIt platform.
