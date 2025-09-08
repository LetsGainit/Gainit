### Services Layer

Describes the business logic and integrations. Each interface under `Interfaces/*` defines contracts consumed by controllers, with concrete implementations under `Implementations/*`.

Areas and Files
- Email
  - `Interfaces/IEmailSender.cs`
  - `Implementations/AcsEmailSender.cs`
- FileUpload
  - `Interfaces/IFileUploadService.cs`
  - `Implementations/FileUploadService.cs`
- Forum
  - `Interfaces/IForumService.cs`, `Interfaces/IForumNotificationService.cs`
  - `Implementations/ForumService.cs`, `Implementations/ForumNotificationService.cs`
- GitHub
  - `Interfaces/IGitHubApiClient.cs`, `Interfaces/IGitHubService.cs`, `Interfaces/IGitHubAnalyticsService.cs`
  - `Implementations/GitHubApiClient.cs`, `Implementations/GitHubService.cs`, `Implementations/GitHubAnalyticsService.cs`, `Implementations/InMemoryGitHubCache.cs`
- Projects
  - `Interfaces/IProjectService.cs`, `IProjectMatchingService.cs`, `IProjectConfigurationService.cs`, `IJoinRequestService.cs`
  - `Implementations/ProjectService.cs`, `ProjectMatchingService.cs`, `ProjectConfigurationService.cs`, `JoinRequestService .cs`
- Tasks
  - `Interfaces/ITaskService.cs`, `IMilestoneService.cs`, `IPlanningService.cs`, `ITaskNotificationService.cs`
  - `Implementations/TaskService.cs`, `MilestoneService.cs`, `PlanningService.cs`, `TaskNotificationService.cs`
- Users
  - `Interfaces/IUserProfileService.cs`, `IUserSummaryService.cs`
  - `Implementations/UserProfileService.cs`, `UserSummaryService.cs`


