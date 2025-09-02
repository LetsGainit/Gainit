# GainIt API Endpoints Documentation

This document provides a comprehensive overview of all API endpoints supported by the GainIt API, organized by controller.

## Table of Contents
- [Users Controller](#users-controller)
- [Projects Controller](#projects-controller)
- [Tasks Controller](#tasks-controller)
- [Milestones Controller](#milestones-controller)
- [Planning Controller](#planning-controller)
- [Forum Controller](#forum-controller)
- [Join Requests Controller](#join-requests-controller)
- [GitHub Controller](#github-controller)

---

## Users Controller
**Base Route:** `api/users`

### User Profile Management
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/me/ensure` | Ensure a local user exists for the current external identity | RequireAccessAsUser |
| GET | `/me` | Get current user profile | RequireAccessAsUser |

### Gainer User Management
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/gainer/{id}/profile` | Get gainer user profile by ID | - |
| POST | `/gainer/{id}/expertise` | Add expertise to gainer user | - |
| POST | `/gainer/{id}/achievements` | Add achievements to gainer user | - |
| PUT | `/gainer/{id}/profile` | Update gainer user profile | - |
| GET | `/gainer/{id}/projects` | Get projects for gainer user | - |
| GET | `/gainer/search` | Search for gainer users | - |

### Mentor User Management
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/mentor/{id}/profile` | Get mentor user profile by ID | - |
| POST | `/mentor/{id}/expertise` | Add expertise to mentor user | - |
| POST | `/mentor/{id}/achievements` | Add achievements to mentor user | - |
| PUT | `/mentor/{id}/profile` | Update mentor user profile | - |
| GET | `/mentor/{id}/projects` | Get projects for mentor user | - |
| GET | `/mentor/search` | Search for mentor users | - |

### Nonprofit User Management
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/nonprofit/{id}/profile` | Get nonprofit user profile by ID | - |
| POST | `/nonprofit/{id}/expertise` | Add expertise to nonprofit user | - |
| POST | `/nonprofit/{id}/achievements` | Add achievements to nonprofit user | - |
| PUT | `/nonprofit/{id}/profile` | Update nonprofit user profile | - |
| GET | `/nonprofit/{id}/projects` | Get projects for nonprofit user | - |
| GET | `/nonprofit/search` | Search for nonprofit users | - |

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

### Nonprofit Projects
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/nonprofit` | Create nonprofit project | - |

### Data Export
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/export-for-azure-vector-search` | Export data for Azure Vector Search | - |

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

---

## Planning Controller
**Base Route:** `api/planning`

### AI Planning
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/generate-roadmap` | Generate project roadmap using AI | - |
| POST | `/{i_TaskId}/elaborate` | Elaborate on specific task using AI | - |

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

---

## GitHub Controller
**Base Route:** `api/github`

### Repository Management
| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/projects/{projectId}/link` | Link GitHub repository to project | - |
| GET | `/projects/{projectId}/repository` | Get project's GitHub repository | - |
| GET | `/repositories/{owner}/{name}/project` | Get project by GitHub repository | - |

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

---

## Authentication & Authorization

The API uses Azure AD B2C for authentication with the following policies:
- `RequireAccessAsUser`: Required for user-specific operations
- Various role-based policies for different user types (Gainers, Mentors, Nonprofits)

## Response Formats

All endpoints return JSON responses with appropriate HTTP status codes:
- `200 OK`: Successful operation
- `201 Created`: Resource created successfully
- `400 Bad Request`: Invalid request data
- `401 Unauthorized`: Authentication required
- `403 Forbidden`: Insufficient permissions
- `404 Not Found`: Resource not found
- `500 Internal Server Error`: Server error

## Rate Limiting

The API implements rate limiting and correlation ID tracking for monitoring and debugging purposes.

## Logging

All endpoints include comprehensive logging with correlation IDs for request tracing and performance monitoring.
