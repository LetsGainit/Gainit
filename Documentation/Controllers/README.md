### Controllers Layer

Describes the HTTP endpoints exposed by the API and maps requests to application services. Each controller below lists its responsibility and the main areas it covers.

Files
- `Forum/ForumController.cs`: Endpoints for project forums: threads, posts, replies, and like operations. Emits realtime notifications where applicable.
- `Projects/GitHubController.cs`: Endpoints for GitHub integration: repository linking, validation, synchronization, analytics retrieval, and status queries.
- `Projects/JoinRequestsController.cs`: Endpoints for project join request lifecycle: create, list, approve/deny, cancel.
- `Projects/ProjectsController.cs`: Endpoints for active and template projects: retrieval, creation, updates, search/match, Azure Vector Search export.
- `Tasks/MilestonesController.cs`: Endpoints to manage milestones and related notifications.
- `Tasks/PlanningController.cs`: Endpoints for AI-assisted planning and roadmap generation.
- `Tasks/TasksController.cs`: Endpoints for tasks, subtasks, dependencies, references, and task notifications.
- `Users/UsersController.cs`: Endpoints for user profiles, expertise management, and profile pictures.

Notes
- Request/response shapes are defined under `DTOs/Requests` and `DTOs/ViewModels`.
- Authentication/authorization is configured centrally; some endpoints require specific policies.


