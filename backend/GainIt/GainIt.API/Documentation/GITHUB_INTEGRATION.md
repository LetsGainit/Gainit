# GitHub Integration for GainIt Platform

## Overview
The GitHub integration enables the GainIt platform to track project analytics and user contributions from GitHub repositories. This integration provides comprehensive insights into project health, user activity, and contribution patterns without requiring complex OAuth flows.

## Architecture

### Core Components
- **GitHubController**: RESTful API endpoints for GitHub operations
- **GitHubService**: Business logic for repository management and analytics
- **GitHubApiClient**: HTTP client for GitHub GraphQL API integration
- **GitHubAnalyticsService**: Data processing and analytics generation

### Database Models
- **User**: Extended with `GitHubUsername` field for contribution mapping
- **GitHubRepository**: Repository metadata and statistics
- **GitHubAnalytics**: Project-level analytics and metrics
- **GitHubContribution**: User contribution tracking and scoring
- **GitHubSyncLog**: Synchronization operation monitoring

## DTO Structure

### Base Classes
The GitHub DTOs use a hierarchical structure to reduce duplication:

- **GitHubBaseResponseDto**: Common properties (ProjectId, DaysPeriod, GeneratedAt)
- **GitHubActivitySummaryBaseDto**: Activity summary responses
- **GitHubMessageResponseDto**: Simple message responses

### Request DTOs
- **GitHubRepositoryLinkDto**: Repository link requests
- **GitHubUrlValidationDto**: URL validation requests

### Response DTOs
- **GitHubRepositoryLinkResponseDto**: Repository link responses
- **GitHubRepositoryInfoDto**: Repository information
- **GitHubProjectAnalyticsResponseDto**: Project analytics responses
- **GitHubUserContributionsResponseDto**: User contributions responses
- **GitHubUserContributionDetailResponseDto**: User contribution detail responses
- **GitHubUserActivitySummaryResponseDto**: User activity summary responses
- **GitHubProjectActivitySummaryResponseDto**: Project activity summary responses
- **GitHubSyncResponseDto**: Sync operation responses
- **GitHubSyncStatusResponseDto**: Sync status responses
- **GitHubUrlValidationResponseDto**: URL validation responses
- **GitHubUserContributionDto**: User contribution data
- **GitHubDetailedContributionDto**: Detailed contribution data
- **GitHubSyncStatusDto**: Sync status data

## Configuration

### GitHubOptions
The `GitHubOptions` class provides centralized configuration for:

- **GitHub App Configuration**: AppId, AppName, PrivateKeyContent
- **API Configuration**: Endpoints, timeouts, rate limiting
- **Sync Configuration**: Sync intervals, batch sizes, retention periods
- **Feature Flags**: Enable/disable specific features
- **Monitoring**: Logging and metrics configuration

### Configuration Files
- **appsettings.GitHub.json**: Template configuration (safe to commit)
- **appsettings.GitHub.Development.json**: Local development secrets (gitignored)
- **Production**: Environment variables on Azure Web App

## API Endpoints

### Repository Management
- `POST /api/github/projects/{projectId}/link`: Link repository to project
- `DELETE /api/github/projects/{projectId}/unlink`: Unlink repository from project
- `POST /api/github/validate-url`: Validate repository URL

### Analytics
- `GET /api/github/projects/{projectId}/analytics`: Get project analytics
- `GET /api/github/projects/{projectId}/contributions`: Get user contributions
- `GET /api/github/projects/{projectId}/contributions/{userId}`: Get user contribution details
- `GET /api/github/projects/{projectId}/stats`: Get repository statistics

### Activity Summaries
- `GET /api/github/projects/{projectId}/users/{userId}/activity-summary`: Get user activity summary
- `GET /api/github/projects/{projectId}/activity-summary`: Get project activity summary

### Synchronization
- `POST /api/github/projects/{projectId}/sync`: Sync GitHub data
- `GET /api/github/projects/{projectId}/sync-status`: Get sync status

## Setup Instructions

### 1. GitHub App Configuration
1. Create a new GitHub App in your GitHub organization
2. Configure the app with appropriate permissions for repository access
3. Generate and download the private key
4. Note the App ID and Installation ID

### 2. Local Development
1. Copy `appsettings.GitHub.json` to `appsettings.GitHub.Development.json`
2. Fill in the actual values for:
   - `AppId`: Your GitHub App ID
   - `PrivateKeyContent`: Your GitHub App private key content
   - `InstallationId`: Your GitHub App installation ID
   - `WebhookSecret`: Your webhook secret (if using webhooks)

### 3. Production Deployment
1. Set the following environment variables in Azure Web App:
   - `GitHub__AppId`: Your GitHub App ID
   - `GitHub__PrivateKeyContent`: Your GitHub App private key content
   - `GitHub__InstallationId`: Your GitHub App installation ID
   - `GitHub__WebhookSecret`: Your webhook secret (if using webhooks)

### 4. Database Migration
1. Run the Entity Framework migration to add the `GitHubUsername` field:
   ```bash
   dotnet ef database update
   ```

## Usage

### User Registration
Users provide their GitHub username during registration, which is stored in the `GitHubUsername` field of the User model.

### Repository Linking
Project owners can link their projects to GitHub repositories using the repository URL. The system validates the URL and stores the repository information.

### Analytics Collection
The system automatically collects analytics data from linked repositories, including:
- Commit statistics
- Issue and pull request metrics
- User contribution data
- Repository health indicators

### ChatGPT Integration
The system generates activity summaries suitable for ChatGPT context, providing insights into user and project activity patterns.

## Features

### Authentication
- GitHub App JWT authentication for API access
- No user OAuth tokens required
- Secure private key storage

### Rate Limiting
- Configurable rate limiting (default: 5,000 requests/hour)
- Automatic rate limit tracking and respect
- Configurable concurrent request limits

### Data Processing
- Batch processing for large datasets
- Configurable retention periods
- Background sync capabilities

### Error Handling
- Comprehensive error handling with appropriate HTTP status codes
- Detailed logging for debugging
- User-friendly error messages

## Benefits

### For Users
- Simple GitHub username registration (no OAuth complexity)
- Automatic analytics collection
- Real-time project insights
- ChatGPT-ready activity summaries

### For Platform
- Scalable architecture with configurable limits
- Secure credential management
- Comprehensive monitoring and logging
- Easy maintenance and updates

### For Developers
- Clean, well-documented API
- Consistent response structures
- Comprehensive Swagger documentation
- Easy testing and debugging

## Security Considerations

- GitHub App private keys stored securely
- No sensitive data in committed configuration files
- Environment-specific secret management
- Proper authentication and authorization

## Performance Features

- Configurable request timeouts
- Batch processing capabilities
- Rate limit awareness
- Efficient data storage and retrieval

## Monitoring and Logging

- Detailed operation logging
- Performance metrics collection
- Error tracking and reporting
- Configurable log levels

## Troubleshooting

### Common Issues
1. **Rate Limit Exceeded**: Check rate limit configuration and reduce request frequency
2. **Authentication Errors**: Verify GitHub App configuration and private key
3. **Repository Not Found**: Ensure repository is public and accessible
4. **Sync Failures**: Check logs for detailed error information

### Debugging
- Enable detailed logging in configuration
- Check GitHub API rate limit headers
- Verify database connection and migrations
- Test with simple repository URLs first

This GitHub integration provides a robust, scalable, and secure solution for tracking project analytics and user contributions, enhancing the GainIt platform with comprehensive GitHub insights.
