# GainIt API Endpoints Documentation

This document provides a comprehensive overview of all API endpoints supported by the GainIt API, organized by controller with detailed request/response examples.

## Table of Contents
- [Users Controller](#users-controller)
- [Projects Controller](#projects-controller)
- [Tasks Controller](#tasks-controller)
- [Milestones Controller](#milestones-controller)
- [Planning Controller](#planning-controller)
- [Forum Controller](#forum-controller)
- [Join Requests Controller](#join-requests-controller)
- [GitHub Controller](#github-controller)
- [Data Types & Enums](#data-types--enums)
- [Error Handling](#error-handling)
- [Authentication & Authorization](#authentication--authorization)

---

## Users Controller
**Base Route:** `api/users`

### User Profile Management
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/me/ensure` | Ensure a local user exists for the current external identity | RequireAccessAsUser |
| GET | `/me` | Get current user profile | RequireAccessAsUser |
| GET | `/me/summary` | Get AI-generated summary of user's activity | RequireAccessAsUser |
| GET | `/{userId}/dashboard` | Get user dashboard analytics data | RequireAccessAsUser |

### Profile Picture Management
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/me/profile-picture` | Upload a new profile picture for the current user | RequireAccessAsUser |
| PUT | `/me/profile-picture` | Update the current user's profile picture | RequireAccessAsUser |
| DELETE | `/me/profile-picture` | Delete the current user's profile picture | RequireAccessAsUser |
| GET | `/me/profile-picture` | Get the current user's profile picture | RequireAccessAsUser |
| GET | `/{userId}/profile-picture` | Get a user's profile picture by user ID (public proxy) | - |

#### POST `/me/ensure`
**Request Body:**
```json
{
  "externalId": "string",
  "email": "string?",
  "fullName": "string?",
  "identityProvider": "string?"
}
```

**Response:** `UserProfileDto`
```json
{
  "userId": "guid",
  "externalId": "string",
  "emailAddress": "string",
  "fullName": "string",
  "country": "string?",
  "gitHubUsername": "string?",
  "isNewUser": "boolean"
}
```

#### GET `/me`
**Response:** `UserProfileDto` (same structure as above)

#### GET `/me/summary`
**Response:** object
```json
{
  "overallSummary": "string"
}
```

#### GET `/{userId}/dashboard`
**Response:** object (analytics data used to build the dashboard)
```json
{
  "userId": "guid",
  "userName": "string",
  "userRole": "string",
  "kpis": {
    "tasksCompleted": { "value": 0, "trend": "string", "subtitle": "string", "icon": "string" },
    "totalHours": { "value": 0, "trend": "string", "subtitle": "string", "icon": "string" },
    "projects": { "value": 0, "trend": "string", "subtitle": "string", "icon": "string" },
    "streak": { "value": 0, "trend": "string", "subtitle": "string", "icon": "string" }
  },
  "skillDistribution": {
    "frontend": { "count": 0, "percentage": 0 },
    "backend": { "count": 0, "percentage": 0 },
    "database": { "count": 0, "percentage": 0 },
    "devops": { "count": 0, "percentage": 0 }
  },
  "activityMetrics": {
    "totalTasks": 0,
    "completedTasks": 0,
    "completionRate": 0,
    "totalProjects": 0,
    "ownedProjects": 0,
    "activeProjects": 0,
    "completedProjects": 0,
    "totalAchievements": 0
  },
  "technologies": {
    "languages": ["string"],
    "technologies": ["string"],
    "totalLanguages": 0,
    "totalTechnologies": 0
  },
  "githubData": {
    "hasUsername": false,
    "username": "string",
    "linkedRepos": 0,
    "hasActivity": false,
    "hasData": false,
    "totalPullRequests": 0,
    "totalCommits": 0,
    "totalCodeReviews": 0,
    "needsSetup": false
  }
}
```

#### POST `/me/profile-picture`
**Request Body:** `ProfilePictureRequestDto` (multipart/form-data)
```json
{
  "profilePicture": "file",
  "description": "string?"
}
```

**Response:** `ProfilePictureResponseViewModel`
```json
{
  "profilePictureUrl": "string",
  "description": "string?",
  "uploadedAt": "datetime",
  "fileSizeInBytes": "integer",
  "fileName": "string",
  "contentType": "string"
}
```

#### PUT `/me/profile-picture`
**Request Body:** `ProfilePictureRequestDto` (multipart/form-data)
- Same as POST above
- Replaces existing profile picture

#### DELETE `/me/profile-picture`
**Response:** Success message string

#### GET `/me/profile-picture`
**Response:** Image file stream with appropriate content type and cache headers

#### GET `/{userId}/profile-picture`
**Response:** Image file stream with appropriate content type and cache headers
- **Note:** This endpoint acts as a proxy to serve images from private Azure Blob Storage
- **Cache Control:** 1 hour public cache with ETag support

### Gainer User Management
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/gainer/{id}/profile` | Get gainer user profile by ID | - |
| POST | `/gainer/{id}/expertise` | Add expertise to gainer user | - |
| POST | `/gainer/{id}/achievements` | Add achievements to gainer user | - |
| PUT | `/gainer/{id}/profile` | Update gainer user profile | - |
| GET | `/gainer/{id}/projects` | Get projects for gainer user | - |
| GET | `/gainer/search` | Search for gainer users | - |

#### POST `/gainer/{id}/expertise`
**Request Body:** `AddTechExpertiseDto`
```json
{
  "programmingLanguages": ["string"],
  "technologies": ["string"],
  "tools": ["string"]
}
```

#### PUT `/gainer/{id}/profile`
**Request Body:** `GainerProfileUpdateDTO`
```json
{
  "fullName": "string",
  "biography": "string",
  "facebookPageURL": "string?",
  "linkedInURL": "string?",
  "gitHubURL": "string?",
  "gitHubUsername": "string?",
  "profilePictureURL": "string?",
  "currentRole": "string",
  "yearsOfExperience": "integer",
  "educationLevel": "string",
  "fieldOfStudy": "string?",
  "institution": "string?"
}
```

### Mentor User Management
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/mentor/{id}/profile` | Get mentor user profile by ID | - |
| POST | `/mentor/{id}/expertise` | Add expertise to mentor user | - |
| POST | `/mentor/{id}/achievements` | Add achievements to mentor user | - |
| PUT | `/mentor/{id}/profile` | Update mentor user profile | - |
| GET | `/mentor/{id}/projects` | Get projects for specific mentor | - |
| GET | `/mentor/search` | Search for mentor users | - |

### Nonprofit User Management
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/nonprofit/{id}/profile` | Get nonprofit user profile by ID | - |
| POST | `/nonprofit/{id}/expertise` | Add expertise to nonprofit user | - |
| POST | `/nonprofit/{id}/achievements` | Add achievements to nonprofit user | - |
| PUT | `/nonprofit/{id}/profile` | Update nonprofit user profile | - |
| GET | `/nonprofit/{id}/projects` | Get projects for specific nonprofit | - |
| GET | `/nonprofit/search` | Search for nonprofit users | - |

#### POST `/nonprofit/{id}/expertise`
**Request Body:** `AddNonprofitExpertiseDto`
```json
{
  "fieldOfWork": "string",
  "missionStatement": "string"
}
```

#### PUT `/nonprofit/{id}/profile`
**Request Body:** `NonprofitProfileUpdateDTO`
```json
{
  "fullName": "string",
  "biography": "string",
  "facebookPageURL": "string?",
  "linkedInURL": "string?",
  "gitHubURL": "string?",
  "gitHubUsername": "string?",
  "profilePictureURL": "string?",
  "websiteUrl": "string",
  "fieldOfWork": "string?",
  "missionStatement": "string?"
}
```

---

## Projects Controller
**Base Route:** `api/projects`

### Project Retrieval
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/{projectId}` | Get project by ID | - |
| GET | `/template/{projectId}` | Get template project by ID | - |
| GET | `/templates` | Get all template projects | - |
| GET | `/nonprofits` | Get nonprofit projects | - |
| GET | `/active` | Get active projects | - |
| GET | `/user/{userId}` | Get projects for specific user | - |
| GET | `/mentor/{mentorId}` | Get projects for specific mentor | - |
| GET | `/nonprofit/{nonprofitId}` | Get projects for specific nonprofit | - |

#### GET `/{projectId}`
**Response:** `UserProjectViewModel`
```json
{
  "projectId": "string",
  "projectName": "string",
  "projectDescription": "string",
  "projectStatus": "string",
  "difficultyLevel": "string",
  "projectSource": "string",
  "createdAtUtc": "datetime",
  "projectTeamMembers": [
    {
      "userId": "string",
      "fullName": "string",
      "profilePictureUrl": "string?",
      "userType": "string",
      "roleInProject": "string"
    }
  ],
  "repositoryLink": "string?",
  "owningOrganization": "object?",
  "assignedMentor": "object?",
  "projectPictureUrl": "string?",
  "duration": "timespan?",
  "openRoles": ["string"],
  "programmingLanguages": ["string"],
  "goals": ["string"],
  "technologies": ["string"]
}
```

### Project Management
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/start-from-template` | Start a new project from template | - |
| PUT | `/{projectId}/mentor` | Update project mentor | - |
| DELETE | `/{projectId}/mentor` | Remove project mentor | - |
| POST | `/{projectId}/team-members` | Add team members to project | - |
| DELETE | `/{projectId}/team-members` | Remove team members from project | - |
| PUT | `/{projectId}/status` | Update project status | - |
| PUT | `/{projectId}/repository` | Update project repository | - |

### Project Search and Discovery
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/search` | Search projects | - |
| GET | `/search/template` | Search template projects | - |
| GET | `/search/vector` | Vector search for projects | - |
| GET | `/match/profile` | Match projects to user profile | - |
| GET | `/projects/filter` | Filter projects | - |
| GET | `/templates/filter` | Filter template projects | - |

#### GET `/search`
**Query Parameters:**
- `q`: Search term
- `difficultyLevel`: Filter by difficulty
- `technologies`: Comma-separated list of technologies
- `status`: Project status filter
- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 20)

### Nonprofit Projects
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/nonprofit` | Create nonprofit project | - |

### Data Export
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/export-for-azure-vector-search` | Export data for Azure Vector Search | - |
| POST | `/export-and-upload` | Export projects and upload to Azure Blob Storage | - |

---

## Tasks Controller
**Base Route:** `api/tasks`

### Task Management
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/{i_TaskId}` | Get task by ID | - |
| GET | `/my-tasks` | Get current user's tasks | - |
| GET | `/board` | Get task board | - |
| POST | `/` | Create new task | - |
| PUT | `/{i_TaskId}` | Update task | - |
| DELETE | `/{i_TaskId}` | Delete task | - |
| PUT | `/{i_TaskId}/status` | Update task status | - |
| PUT | `/{i_TaskId}/order` | Update task order | - |

#### GET `/my-tasks`
**Query Parameters:** `TaskListQueryDto`
- `type`: Task type filter
- `priority`: Priority filter
- `milestoneId`: Filter by milestone
- `searchTerm`: Search in task titles/descriptions
- `sortBy`: Sort field (OrderIndex, CreatedAtUtc, DueAtUtc, Priority)
- `sortDescending`: Sort direction (default: false)

#### GET `/board`
**Query Parameters:** `TaskBoardQueryDto`
- `type`: Task type filter
- `priority`: Priority filter
- `milestoneId`: Filter by milestone
- `assignedRole`: Filter by assigned role
- `assignedUserId`: Filter by assigned user
- `isBlocked`: Filter by blocked status
- `searchTerm`: Search term
- `includeCompleted`: Include completed tasks (default: false)
- `sortBy`: Sort field
- `sortDescending`: Sort direction

#### POST `/`
**Request Body:** `ProjectTaskCreateDto`
```json
{
  "title": "string",
  "description": "string?",
  "type": "Feature|Research|Infra|Docs|Refactor",
  "priority": "Low|Medium|High|Critical",
  "dueAtUtc": "datetime?",
  "milestoneId": "guid?",
  "assignedRole": "string?",
  "assignedUserId": "guid?",
  "orderIndex": "integer?",
  "subtasks": [
    {
      "title": "string",
      "description": "string?",
      "orderIndex": "integer?"
    }
  ]
}
```

**Response:** `ProjectTaskViewModel`
```json
{
  "taskId": "guid",
  "title": "string",
  "description": "string?",
  "status": "Todo|InProgress|Done|Blocked",
  "priority": "Low|Medium|High|Critical",
  "type": "Feature|Research|Infra|Docs|Refactor",
  "isBlocked": "boolean",
  "orderIndex": "integer",
  "createdAtUtc": "datetime",
  "dueAtUtc": "datetime?",
  "assignedRole": "string?",
  "assignedUserId": "guid?",
  "milestoneId": "guid?",
  "milestoneTitle": "string?",
  "subtaskCount": "integer",
  "completedSubtaskCount": "integer",
  "subtasks": [
    {
      "subtaskId": "guid",
      "title": "string",
      "description": "string?",
      "isDone": "boolean",
      "orderIndex": "integer",
      "completedAtUtc": "datetime?"
    }
  ],
  "references": [
    {
      "referenceId": "guid",
      "type": "integer",
      "url": "string",
      "title": "string?",
      "createdAtUtc": "datetime"
    }
  ],
  "dependencies": [
    {
      "taskId": "guid",
      "dependsOnTaskId": "guid",
      "dependsOnTitle": "string",
      "dependsOnStatus": "integer"
    }
  ]
}
```

#### PUT `/{i_TaskId}`
**Request Body:** `ProjectTaskUpdateDto`
```json
{
  "title": "string?",
  "description": "string?",
  "type": "Feature|Research|Infra|Docs|Refactor?",
  "priority": "Low|Medium|High|Critical?",
  "dueAtUtc": "datetime?",
  "milestoneId": "guid?",
  "assignedRole": "string?",
  "assignedUserId": "guid?"
}
```

#### PUT `/{i_TaskId}/status`
**Request Body:** `eTaskStatus` enum value
```json
"Todo" | "InProgress" | "Done" | "Blocked"
```

### Subtask Management
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/{i_TaskId}/subtasks` | Get subtasks for task | - |
| POST | `/{i_TaskId}/subtasks` | Create subtask for task | - |
| PUT | `/{i_TaskId}/subtasks/{subtaskId}` | Update subtask | - |
| DELETE | `/{i_TaskId}/subtasks/{subtaskId}` | Delete subtask | - |
| PUT | `/{i_TaskId}/subtasks/{subtaskId}/toggle` | Toggle subtask completion | - |

### Task Dependencies
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/{i_TaskId}/dependencies` | Add task dependency | - |
| DELETE | `/{i_TaskId}/dependencies/{dependsOnTaskId}` | Remove task dependency | - |

### Task References
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/{i_TaskId}/references` | Get task references | - |
| POST | `/{i_TaskId}/references` | Add task reference | - |
| DELETE | `/{i_TaskId}/references/{referenceId}` | Remove task reference | - |

---

## Milestones Controller
**Base Route:** `api/milestones`

### Milestone Management
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/` | Get all milestones | - |
| GET | `/{milestoneId}` | Get milestone by ID | - |
| POST | `/` | Create new milestone | - |
| PUT | `/{milestoneId}` | Update milestone | - |
| DELETE | `/{milestoneId}` | Delete milestone | - |
| PUT | `/{milestoneId}/status` | Update milestone status | - |

#### GET `/{milestoneId}`
**Response:** `ProjectMilestoneViewModel`
```json
{
  "milestoneId": "guid",
  "title": "string",
  "description": "string?",
  "status": "Planned|Active|Completed",
  "tasksCount": "integer",
  "doneTasksCount": "integer",
  "orderIndex": "integer",
  "targetDateUtc": "datetime?"
}
```

---

## Planning Controller
**Base Route:** `api/planning`

### AI Planning
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/generate-roadmap` | Generate project roadmap using AI | - |
| POST | `/{i_TaskId}/elaborate` | Elaborate on specific task using AI | - |

#### POST `/generate-roadmap`
**Request Body:** `PlanRequestDto`
```json
{
  "goal": "string?",
  "constraints": "string?",
  "preferredTechnologies": "string?",
  "startDateUtc": "datetime?",
  "targetDueDateUtc": "datetime?"
}
```

**Response:** `PlanApplyResultViewModel`
```json
{
  "projectId": "guid",
  "createdMilestones": [
    {
      "milestoneId": "guid",
      "title": "string",
      "description": "string?",
      "status": "Planned|Active|Completed",
      "tasksCount": "integer",
      "doneTasksCount": "integer",
      "orderIndex": "integer",
      "targetDateUtc": "datetime?"
    }
  ],
  "createdTasks": ["ProjectTaskViewModel"],
  "notes": ["string"]
}
```

#### POST `/{i_TaskId}/elaborate`
**Response:** `TaskElaborationResultViewModel`
```json
{
  "projectId": "guid",
  "taskId": "guid",
  "notes": ["string"]
}
```

---

## Forum Controller
**Base Route:** `api/forum`

### Forum Posts
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/posts` | Create new forum post | - |
| GET | `/posts/{postId}` | Get forum post by ID | - |
| GET | `/projects/{projectId}/posts` | Get posts for specific project | - |
| PUT | `/posts/{postId}` | Update forum post | - |
| DELETE | `/posts/{postId}` | Delete forum post | - |

#### GET `/posts/{postId}`
**Response:** `ForumPostViewModel`
```json
{
  "postId": "guid",
  "projectId": "guid",
  "authorId": "guid",
  "authorName": "string",
  "authorRole": "string",
  "content": "string",
  "createdAtUtc": "datetime",
  "updatedAtUtc": "datetime?",
  "likeCount": "integer",
  "replyCount": "integer",
  "isLikedByCurrentUser": "boolean",
  "replies": [
    {
      "replyId": "guid",
      "postId": "guid",
      "authorId": "guid",
      "authorName": "string",
      "authorRole": "string",
      "content": "string",
      "createdAtUtc": "datetime",
      "updatedAtUtc": "datetime?",
      "likeCount": "integer",
      "isLikedByCurrentUser": "boolean"
    }
  ]
}
```

### Forum Replies
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/replies` | Create new forum reply | - |
| PUT | `/replies/{replyId}` | Update forum reply | - |
| DELETE | `/replies/{replyId}` | Delete forum reply | - |

### Forum Interactions
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/posts/{postId}/like` | Like/unlike forum post | - |
| POST | `/replies/{replyId}/like` | Like/unlike forum reply | - |

---

## Join Requests Controller
**Base Route:** `api/join-requests`

### Join Request Management
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/createrequest` | Create new join request | - |
| GET | `/{joinRequestId:guid}` | Get join request by ID | - |
| GET | `/myrequests` | Get current user's join requests | - |
| POST | `/{joinRequestId:guid}/cancel` | Cancel join request | - |
| POST | `/{joinRequestId:guid}/decision` | Make decision on join request | - |

#### POST `/createrequest`
**Request Body:** `JoinRequestCreateDto`
```json
{
  "message": "string?",
  "requestedRole": "string"
}
```

#### GET `/{joinRequestId:guid}`
**Response:** `JoinRequestViewModel`
```json
{
  "joinRequestId": "guid",
  "projectId": "guid",
  "requesterUserId": "guid",
  "requesterFullName": "string",
  "requesterEmailAddress": "string",
  "message": "string?",
  "status": "string",
  "decisionReason": "string?",
  "createdAtUtc": "datetime",
  "decisionAtUtc": "datetime?",
  "isApproved": "boolean",
  "requestedRole": "string",
  "projectName": "string?",
  "deciderUserId": "guid?"
}
```

---

## GitHub Controller
**Base Route:** `api/github`

### Repository Management
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/projects/{projectId}/link` | Link GitHub repository to project | - |
| GET | `/projects/{projectId}/repository` | Get project's GitHub repository | - |
| GET | `/repositories/{owner}/{name}/project` | Get project by GitHub repository | - |

#### POST `/projects/{projectId}/link`
**Request Body:** `GitHubRepositoryLinkDto`
```json
{
  "repositoryUrl": "string"
}
```

**Response:** `GitHubRepositoryLinkResponseDto`
```json
{
  "projectId": "guid",
  "repositoryId": "string",
  "repositoryName": "string",
  "ownerName": "string",
  "fullName": "string",
  "description": "string?",
  "isPublic": "boolean",
  "primaryLanguage": "string?",
  "languages": ["string"],
  "starsCount": "integer",
  "forksCount": "integer"
}
```

### GitHub Analytics
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/projects/{projectId}/stats` | Get GitHub statistics for project | - |
| GET | `/projects/{projectId}/analytics` | Get GitHub analytics for project | - |
| GET | `/projects/{projectId}/contributions` | Get GitHub contributions for project | - |
| GET | `/projects/{projectId}/users/{userId}/contributions` | Get user contributions to project | - |
| GET | `/projects/{projectId}/users/{userId}/activity` | Get user activity in project | - |
| GET | `/projects/{projectId}/activity-summary` | Get project activity summary | - |
| GET | `/projects/{projectId}/insights` | Get project insights | - |

### GitHub Synchronization
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/projects/{projectId}/sync` | Sync project with GitHub | - |
| GET | `/projects/{projectId}/sync-status` | Get GitHub sync status | - |

### URL Validation
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/validate-url` | Validate GitHub URL | - |

#### POST `/validate-url`
**Request Body:** `GitHubUrlValidationDto`
```json
{
  "url": "string"
}
```

---

## Data Types & Enums

### Task Status (`eTaskStatus`)
- `Todo` - Task is not started
- `InProgress` - Task is currently being worked on
- `Done` - Task is completed
- `Blocked` - Task is blocked by dependencies

### Task Priority (`eTaskPriority`)
- `Low` - Low priority task
- `Medium` - Medium priority task (default)
- `High` - High priority task
- `Critical` - Critical priority task

### Task Type (`eTaskType`)
- `Feature` - New feature implementation
- `Research` - Research and analysis
- `Infra` - Infrastructure work
- `Docs` - Documentation
- `Refactor` - Code refactoring

### Project Status (`eProjectStatus`)
- `NotActive` - Project is not active
- `Pending` - Project is pending approval
- `InProgress` - Project is currently active
- `Completed` - Project is completed

### Milestone Status (`eMilestoneStatus`)
- `Planned` - Milestone is planned
- `Active` - Milestone is currently active
- `Completed` - Milestone is completed

---

## Error Handling

### Error Response Format
All error responses follow this structure:
```json
{
  "error": "string"
}
```

### HTTP Status Codes
- `200 OK`: Successful operation
- `201 Created`: Resource created successfully
- `400 Bad Request`: Invalid request data or validation errors
- `401 Unauthorized`: Authentication required
- `403 Forbidden`: Insufficient permissions
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

### Common Error Scenarios
- **Validation Errors**: When request data fails validation (400)
- **Authentication Required**: When accessing protected endpoints without valid token (401)
- **Resource Not Found**: When requesting non-existent resources (404)
- **Permission Denied**: When user lacks required permissions (403)

---

## Authentication & Authorization

The API uses Azure AD B2C for authentication with the following policies:
- `RequireAccessAsUser`: Required for user-specific operations
- Various role-based policies for different user types (Gainers, Mentors, Nonprofits)

### Authentication Flow
1. Frontend obtains access token from Azure AD B2C
2. Include token in `Authorization` header: `Bearer {token}`
3. API validates token and extracts user claims
4. Authorization policies check user permissions

---

## Response Formats

All endpoints return JSON responses with appropriate HTTP status codes. Response bodies are structured according to the DTOs defined in the codebase.

### Pagination
For list endpoints that support pagination:
- Use `page` and `pageSize` query parameters
- Response may include pagination metadata (implementation varies by endpoint)

### Date Format
All dates are returned in ISO 8601 format with UTC timezone: `YYYY-MM-DDTHH:mm:ss.sssZ`

---

## Rate Limiting

The API implements rate limiting and correlation ID tracking for monitoring and debugging purposes.

### Rate Limit Headers
- `X-RateLimit-Limit`: Maximum requests per time window
- `X-RateLimit-Remaining`: Remaining requests in current window
- `X-RateLimit-Reset`: Time when rate limit resets

---

## Logging

All endpoints include comprehensive logging with correlation IDs for request tracing and performance monitoring.

### Correlation ID
- Include `X-Correlation-ID` header in requests for tracing
- API generates correlation ID if not provided
- Use for debugging and support requests

---

## Development Notes

### Testing Endpoints
- Use the provided HTTP file (`GainIt.API.http`) for testing
- All endpoints are documented in Swagger/OpenAPI when available
- Test with valid authentication tokens

### Data Validation
- Request DTOs include validation attributes
- API returns 400 Bad Request for validation failures
- Check error response for specific validation details

### Best Practices
- Always handle error responses gracefully
- Include proper authentication headers
- Use appropriate HTTP methods for operations
- Follow REST conventions for resource naming

### Azure Blob Storage Integration
- **Profile Pictures**: Stored in private `profile-pictures` container
- **Project Exports**: Stored in private `projects` container
- **Security**: All containers are private; images served via API proxy
- **File Validation**: Supports JPG, JPEG, PNG, GIF, WebP formats up to 10MB
- **Cache Control**: Profile pictures cached for 1 hour with ETag support
