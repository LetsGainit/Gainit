# GainIt 🚀

A comprehensive collaborative platform that connects **Gainers** (developers), **Mentors** (experienced developers), and **Nonprofit Organizations** to work together on meaningful projects with advanced project management, GitHub integration, and task tracking capabilities.

## 🌟 Overview

GainIt is a .NET 8 Web API that facilitates collaboration between different user types in the software development ecosystem. The platform enables:

- **Gainers**: Developers looking to gain experience and build their portfolios
- **Mentors**: Experienced developers who guide and support projects
- **Nonprofit Organizations**: Organizations that need software solutions developed

## 🎯 Key Features

### User Management
- **Three User Types**: Gainer, Mentor, and Nonprofit Organization
- **Profile Management**: Complete user profiles with biographies, social links, and achievements
- **Expertise Tracking**: Technical and nonprofit expertise tracking with specialized models
- **Achievement System**: Track and display user accomplishments
- **User Search & Discovery**: Find gainers, mentors, and nonprofits by various criteria

### Project Management
- **Template Projects**: Pre-defined project templates for quick start
- **Active Projects**: Real-time project tracking with comprehensive status management
- **Team Collaboration**: Add/remove team members and mentors with role-based access
- **Join Request System**: Streamlined process for users to join projects
- **Project Matching**: Intelligent matching between projects and users
- **Project Filtering**: Advanced search and filter by status, difficulty, type, and more

### GitHub Integration 🔗
- **Repository Linking**: Connect projects to GitHub repositories
- **Analytics & Insights**: Comprehensive repository analytics with contribution tracking
- **User Contributions**: Track individual developer contributions across projects
- **Repository Statistics**: Monitor stars, forks, issues, pull requests, and languages
- **Activity Summaries**: AI-powered summaries of project and user activity
- **Sync Management**: Automated and manual synchronization of GitHub data
- **Public Repository Support**: Full integration with public GitHub repositories

### Task Management System 📋
- **Project Tasks**: Create and manage tasks with priorities, types, and statuses
- **Milestones**: Organize tasks into project milestones
- **Subtasks**: Break down complex tasks into manageable subtasks
- **Task Dependencies**: Define relationships between tasks
- **Planning Services**: Advanced project planning and roadmap management
- **Task Notifications**: Real-time notifications for task updates

### Communication & Notifications
- **Email Services**: Integrated email notifications via Azure Communication Services
- **Real-time Updates**: Live updates for project and task changes
- **Join Request Management**: Streamlined approval/rejection workflow

## 🛠 Technology Stack

- **Framework**: .NET 8 Web API
- **Database**: PostgreSQL with Entity Framework Core
- **ORM**: Entity Framework Core 9.0.3
- **Authentication**: JWT-based authentication with role-based access control
- **GitHub Integration**: GitHub REST API for public repository access
- **Email Services**: Azure Communication Services for notifications
- **Real-time Communication**: SignalR for live updates
- **Documentation**: Swagger/OpenAPI
- **Architecture**: Clean Architecture with Service Layer Pattern

## 📁 Project Structure

```
GainIt/
├── GainIt.API/
│   ├── Controllers/           # API endpoints
│   │   ├── Projects/         # Project and GitHub management endpoints
│   │   └── Users/            # User management endpoints
│   ├── Data/                 # Database context and migrations
│   ├── DTOs/                 # Data Transfer Objects
│   │   ├── Requests/         # Request models (GitHub, Projects, Tasks, Users)
│   │   ├── Search/          # Search-specific DTOs
│   │   └── ViewModels/      # Response models (GitHub, Projects, Tasks, Users)
│   ├── Models/               # Domain models
│   │   ├── Enums/           # Enumerations (Projects, Tasks, Users)
│   │   ├── Projects/        # Project-related models including GitHub models
│   │   ├── Tasks/           # Task management models
│   │   └── Users/           # User-related models with expertise tracking
│   ├── Services/             # Business logic layer
│   │   ├── Email/           # Email notification services
│   │   ├── GitHub/          # GitHub integration services
│   │   ├── Projects/        # Project management services
│   │   ├── Tasks/           # Task management services
│   │   └── Users/           # User profile services
│   ├── Middleware/           # Custom middleware
│   ├── HealthChecks/         # Health monitoring
│   ├── Realtime/            # SignalR hubs
│   └── Documentation/        # API documentation and guides
```

## 🚀 Getting Started

### Prerequisites

- .NET 8 SDK
- PostgreSQL database
- Visual Studio 2022 or VS Code
- Azure Communication Services account (for email notifications)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/LetsGainit/Gainit.git
   cd Gainit
   ```

2. **Configure the database**
   - Update the connection string in `appsettings.json`
   - Run Entity Framework migrations:
   ```bash
   cd backend/GainIt/GainIt.API
   dotnet ef database update
   ```

3. **Configure external services**
   - Set up Azure Communication Services for email notifications
   - Configure GitHub API access (no authentication required for public repos)
   - Update `appsettings.json` with your service configurations

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access the API**
   - API Base URL: `https://localhost:7001`
   - Swagger Documentation: `https://localhost:7001/swagger`

### Environment Configuration

Key configuration sections in `appsettings.json`:
- **ConnectionStrings**: Database connection
- **AzureCommunicationServices**: Email service configuration
- **GitHub**: API rate limiting and cache settings
- **Logging**: Structured logging configuration

## 📚 API Endpoints

### Users Management
- `POST /api/users/me/ensure` - Ensure user exists in system
- `GET /api/users/gainer/{id}/profile` - Get Gainer profile
- `GET /api/users/mentor/{id}/profile` - Get Mentor profile  
- `GET /api/users/nonprofit/{id}/profile` - Get Nonprofit profile
- `PUT /api/users/gainer/{id}/profile` - Update Gainer profile
- `PUT /api/users/mentor/{id}/profile` - Update Mentor profile
- `PUT /api/users/nonprofit/{id}/profile` - Update Nonprofit profile
- `POST /api/users/{userType}/{id}/expertise` - Add user expertise
- `POST /api/users/{userType}/{id}/achievements` - Add user achievements
- `GET /api/users/{userType}/{id}/projects` - Get user projects
- `GET /api/users/{userType}/search` - Search users by type

### Projects Management
- `GET /api/projects/{projectId}` - Get active project
- `GET /api/projects/template/{projectId}` - Get template project
- `GET /api/projects/templates` - Get all template projects
- `GET /api/projects/nonprofits` - Get nonprofit projects
- `GET /api/projects/active` - Get all active projects
- `POST /api/projects/start-from-template` - Create project from template
- `POST /api/projects/nonprofit` - Create nonprofit project
- `PUT /api/projects/{projectId}/mentor` - Assign mentor to project
- `POST /api/projects/{projectId}/team-members` - Add team member
- `PUT /api/projects/{projectId}/status` - Update project status
- `GET /api/projects/search` - Search projects
- `GET /api/projects/filter` - Filter projects by criteria

### Join Requests System
- `POST /api/projects/{projectId}/createrequest` - Create join request
- `GET /api/projects/{projectId}/{joinRequestId}` - Get join request details
- `GET /api/projects/{projectId}/myrequests` - Get user's join requests
- `POST /api/projects/{projectId}/{joinRequestId}/cancel` - Cancel join request
- `POST /api/projects/{projectId}/{joinRequestId}/decision` - Approve/reject join request

### GitHub Integration
- `POST /api/github/projects/{projectId}/link` - Link GitHub repository
- `GET /api/github/projects/{projectId}/repository` - Get repository info
- `GET /api/github/projects/{projectId}/stats` - Get repository statistics
- `GET /api/github/projects/{projectId}/analytics` - Get project analytics
- `GET /api/github/projects/{projectId}/contributions` - Get user contributions
- `GET /api/github/projects/{projectId}/users/{userId}/contributions` - Get specific user contributions
- `GET /api/github/projects/{projectId}/users/{userId}/activity` - Get user activity summary
- `GET /api/github/projects/{projectId}/activity-summary` - Get project activity summary
- `GET /api/github/projects/{projectId}/insights` - Get personalized analytics insights
- `POST /api/github/projects/{projectId}/sync` - Sync GitHub data
- `GET /api/github/projects/{projectId}/sync-status` - Get sync status
- `POST /api/github/validate-url` - Validate GitHub repository URL
- `GET /api/github/repositories/{owner}/{name}/project` - Get project by repository

## 🗄 Database Schema

The application uses PostgreSQL with the following key entities:

### User Management
- **Users**: Base user information and profiles
- **Gainers**: Developers seeking experience with education status and interests
- **Mentors**: Experienced developers providing guidance with expertise areas
- **NonprofitOrganizations**: Organizations needing software solutions
- **UserExpertise**: Technical and nonprofit expertise tracking
- **UserAchievements**: Achievement tracking system
- **AchievementTemplate**: Template definitions for achievements

### Project Management
- **UserProjects**: Active projects with comprehensive metadata
- **TemplateProjects**: Project templates for quick start
- **ProjectMembers**: Team member relationships with roles
- **JoinRequest**: Join request system for project participation

### Task Management System
- **ProjectTask**: Individual tasks with types, priorities, and statuses
- **ProjectMilestone**: Project milestones containing multiple tasks
- **ProjectSubtask**: Breakdown of complex tasks
- **TaskDependency**: Dependencies between tasks
- **ProjectTaskReference**: External references for tasks

### GitHub Integration
- **GitHubRepository**: Repository metadata, languages, branches, statistics
- **GitHubAnalytics**: Aggregated metrics with weekly/monthly breakdowns
- **GitHubContribution**: Per-user contribution snapshots and metrics
- **GitHubSyncLog**: Sync operation status and history

## 🔧 Development

### Adding New Features
1. Create models in the appropriate `Models/` directory (Projects, Tasks, Users)
2. Add DTOs in `DTOs/` for API contracts (Requests and ViewModels)
3. Implement services in `Services/` for business logic
4. Create controllers in `Controllers/` for API endpoints
5. Update database with migrations
6. Add appropriate tests

### Key Development Areas

#### GitHub Integration
- **GitHubService**: Core GitHub operations and repository management
- **GitHubApiClient**: HTTP client for GitHub REST API interactions
- **GitHubAnalyticsService**: Analytics aggregation and contribution calculations
- GitHub DTOs follow a structured hierarchy with base classes for consistency

#### Task Management
- **TaskService**: Task CRUD operations and status management
- **MilestoneService**: Milestone management and task organization
- **PlanningService**: Project planning and roadmap generation
- **TaskNotificationService**: Real-time task update notifications

#### Project Matching
- **ProjectMatchingService**: Intelligent matching between users and projects
- Advanced filtering and search capabilities
- AI-powered insights and recommendations

### Code Style
- Follow C# naming conventions
- Use async/await for database operations
- Implement proper error handling with structured logging
- Add XML documentation for public APIs
- Use dependency injection for service registration
- Follow Clean Architecture principles

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with .NET 8 and Entity Framework Core
- PostgreSQL for reliable data storage
- GitHub REST API for repository integration
- Azure Communication Services for email notifications
- Swagger for comprehensive API documentation
- SignalR for real-time communication

## 📖 Additional Resources

- [GitHub Integration Guide](backend/GainIt/GainIt.API/Documentation/GITHUB_INTEGRATION.md)
- [GitHub Implementation Summary](backend/GainIt/GainIt.API/Documentation/GITHUB_IMPLEMENTATION_SUMMARY.md)
- [User Profile API Documentation](backend/GainIt/GainIt.API/Documentation/USER_PROFILE_UPDATE_API.md)
- [Logging Documentation](backend/GainIt/GainIt.API/Documentation/LOGGING_DOCUMENTATION/)

---

**GainIt** - Connecting developers, mentors, and nonprofits for meaningful software projects with comprehensive project management and GitHub integration! 🚀
