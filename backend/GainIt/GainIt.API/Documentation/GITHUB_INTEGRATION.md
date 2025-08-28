# GitHub Integration for GainIt Platform

## Overview
The GitHub integration enables the GainIt platform to track project analytics and user contributions from public GitHub repositories. It uses the GitHub REST API without user OAuth or GitHub App authentication.

## Current Status
Implemented for public repositories via REST. Core services, models, and API endpoints are available and documented in Swagger.

## Architecture

### Core Components
- **GitHubController**: REST API endpoints for repository linking, analytics, contributions, summaries, and sync status
- **GitHubService**: Orchestrates repository data, analytics refresh, contributions, and summaries
- **GitHubApiClient**: HTTP client for GitHub REST API
- **GitHubAnalyticsService**: Aggregates repository analytics and user contributions

### Database Models
- **User**: Includes `GitHubUsername` for contribution mapping (fallback to `GitHubURL` username extraction)
- **GitHubRepository**: Repository metadata, languages, branches, star/fork/issue counts, last activity
- **GitHubAnalytics**: Aggregated analytics (weekly/monthly breakdowns)
- **GitHubContribution**: Per-user contribution snapshots and metrics
- **GitHubSyncLog**: Sync operation status and history

## DTO Structure

### Base Classes
- **GitHubBaseResponseDto**: Common properties (`ProjectId`, `DaysPeriod`, `GeneratedAt`)
- **GitHubActivitySummaryBaseDto**: Activity summary wrapper
- **GitHubMessageResponseDto**: Message-only responses

### Request DTOs
- **GitHubRepositoryLinkDto**: Repository link requests
- **GitHubUrlValidationDto**: URL validation requests

### Response DTOs
- **GitHubRepositoryLinkResponseDto**
- **GitHubRepositoryInfoDto**
- **GitHubProjectAnalyticsResponseDto**
- **GitHubUserContributionsResponseDto**
- **GitHubUserContributionDetailResponseDto**
- **GitHubUserActivitySummaryResponseDto**
- **GitHubSyncResponseDto**
- **SyncStatusResponseDto** (wraps **GitHubSyncStatusDto**)
- **UrlValidationResponseDto**
- **GitHubUserContributionDto**
- **GitHubDetailedContributionDto**

## Configuration
No GitHub App configuration is required. The integration accesses public repositories via the GitHub REST API.

## API Endpoints

### Repository Management
- `POST /api/github/projects/{projectId}/link`: Link repository to project
- `GET /api/github/projects/{projectId}/repository`: Get linked repository information
- `POST /api/github/validate-url`: Validate repository URL

### Analytics & Statistics
- `GET /api/github/projects/{projectId}/analytics`: Get project analytics (auto-create/refresh as needed)
- `GET /api/github/projects/{projectId}/contributions`: Get user contributions
- `GET /api/github/projects/{projectId}/users/{userId}/contributions`: Get a specific user’s contribution details
- `GET /api/github/projects/{projectId}/stats`: Get repository statistics, languages, branches, and top contributors

### Activity Summaries
- `GET /api/github/projects/{projectId}/users/{userId}/activity`: Get user activity summary
- `GET /api/github/projects/{projectId}/activity-summary`: Get project activity summary

### Synchronization
- `POST /api/github/projects/{projectId}/sync`: Trigger sync (`repository`, `analytics`, or `all`)
- `GET /api/github/projects/{projectId}/sync-status`: Get last sync status

## Behavior

- Public repository access using GitHub REST API
- Repository link validation ensures URL format and accessibility
- Repository metadata includes stars, forks, issues, languages, and branches
- Analytics auto-creation/refresh with freshness window (~1 day)
- Contributions computed for active project members using `GitHubUsername` (fallback to `GitHubURL` username)
- Sync status recorded in `GitHubSyncLog`

## Testing

1. Start the application
2. Use Swagger UI at `/swagger`
3. Link a public repository and fetch analytics, contributions, stats, and summaries

## Troubleshooting

- Ensure the repository is public and the URL is valid
- Respect GitHub’s unauthenticated REST rate limits
- Check application logs for details during sync and analytics generation
