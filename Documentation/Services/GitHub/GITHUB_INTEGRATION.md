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

### Comprehensive Overview (Recommended)
- `GET /api/github/projects/{projectId}/overview`: Get comprehensive GitHub project overview with all data in one call
  - **Query Parameters:** `daysPeriod` (1-365, default: 30)
  - **Returns:** Repository info, statistics, analytics, contributions, activity summary, and sync status
  - **Performance:** Parallel data fetching for optimal performance
  - **Use Case:** Perfect for dashboard implementations

### Repository Management
- `POST /api/github/projects/{projectId}/link`: Link repository to project
- `GET /api/github/projects/{projectId}/repository`: Get linked repository information
- `GET /api/github/repositories/{owner}/{name}/project`: Resolve repository to project ID
- `POST /api/github/validate-url`: Validate repository URL

### Analytics & Statistics
- `GET /api/github/projects/{projectId}/analytics`: Get project analytics (auto-create/refresh as needed)
- `GET /api/github/projects/{projectId}/contributions`: Get user contributions
- `GET /api/github/projects/{projectId}/users/{userId}/contributions`: Get a specific user's contribution details
- `GET /api/github/projects/{projectId}/stats`: Get repository statistics, languages, branches, and top contributors

### Activity Summaries & AI Insights
- `GET /api/github/projects/{projectId}/users/{userId}/activity`: Get user activity summary
- `GET /api/github/projects/{projectId}/activity-summary`: Get project activity summary
- `GET /api/github/projects/{projectId}/insights?userQuery=...`: Get personalized analytics insights from a query

### Synchronization
- `POST /api/github/projects/{projectId}/sync`: Trigger sync (`repository`, `analytics`, or `all`)
- `GET /api/github/projects/{projectId}/sync-status`: Get last sync status

## AI Summaries

- User and project summaries are generated from repository analytics and contribution data.
- Endpoints: user activity (`/users/{userId}/activity`), project activity (`/activity-summary`), and personalized insights (`/insights?userQuery=...`).
- All AI-generated content is included in the comprehensive overview endpoint for convenience.

## User Data Integration

### User Summary & Dashboard
- **User Summary**: AI-generated summary of user's platform activity and GitHub contributions
  - **Endpoint**: `GET /api/users/me/summary`
  - **Cache Duration**: 1 hour
  - **Cache Busting**: Use `?forceRefresh=true` to bypass cache
- **User Dashboard**: Comprehensive analytics data for dashboard UI
  - **Endpoint**: `GET /api/users/{userId}/dashboard`
  - **Cache**: Not cached (always fresh)
  - **Cache Busting**: Use `?forceRefresh=true` for explicit refresh headers

### Cache Busting Parameters
Both user endpoints support cache busting via query parameters:
- `forceRefresh=true`: Bypasses cache and forces fresh data generation
- Adds HTTP headers: `Cache-Control: no-cache, no-store, must-revalidate`
- Useful for development, testing, and ensuring data freshness

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
