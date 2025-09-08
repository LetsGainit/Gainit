### Domain Models

Describes core entities and enums used across the application.

Structure
- `Enums/`
  - `Projects/`: `eDifficultyLevel`, `eJoinRequestStatus`, `eProjectSource`, `eProjectStatus`
  - `Tasks/`: `eMilestoneStatus`, `eTaskPriority`, `eTaskReferenceType`, `eTaskStatus`, `eTaskType`
  - `Users/`: `eUserType`
- `ProjectForum/`: `ForumPost`, `ForumReply`, `ForumPostLike`, `ForumReplyLike`
- `Projects/`: `GitHubRepository`, `GitHubAnalytics`, `GitHubContribution`, `GitHubSyncLog`, `ProjectMember`, `JoinRequest`, `RagContext`, `TemplateProject`, `UserProject`
- `Tasks/`: `ProjectTask`, `ProjectSubtask`, `ProjectMilestone`, `ProjectTaskReference`, `TaskDependency`, `AIPlanning/*`
- `Users/`: `User`, `Gainers/Gainer`, `Mentors/Mentor`, `Nonprofits/NonprofitOrganization`, `Expertise/*`, `AchievementTemplate`, `UserAchievement`


