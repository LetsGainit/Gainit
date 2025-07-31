# GainIt 🚀

A collaborative platform that connects **Gainers** (developers), **Mentors** (experienced developers), and **Nonprofit Organizations** to work together on meaningful projects.

## 🌟 Overview

GainIt is a .NET 8 Web API that facilitates collaboration between different user types in the software development ecosystem. The platform enables:

- **Gainers**: Developers looking to gain experience and build their portfolios
- **Mentors**: Experienced developers who guide and support projects
- **Nonprofit Organizations**: Organizations that need software solutions developed

## 🎯 Key Features

### User Management
- **Three User Types**: Gainer, Mentor, and Nonprofit Organization
- **Profile Management**: Complete user profiles with biographies, social links, and achievements
- **Expertise Tracking**: Technical and nonprofit expertise tracking
- **Achievement System**: Track and display user accomplishments

### Project Management
- **Template Projects**: Pre-defined project templates for quick start
- **Active Projects**: Real-time project tracking with status management
- **Team Collaboration**: Add/remove team members and mentors
- **Repository Integration**: Link projects to external repositories
- **Project Filtering**: Search and filter by status, difficulty, and type

### Advanced Features
- **Project Status Tracking**: Monitor project progress (Active, Completed, etc.)
- **Difficulty Levels**: Categorized project complexity
- **Search & Filter**: Find projects by name, description, or criteria
- **Nonprofit Integration**: Specialized project creation for nonprofit organizations

## 🛠 Technology Stack

- **Framework**: .NET 8 Web API
- **Database**: PostgreSQL with Entity Framework Core
- **ORM**: Entity Framework Core 9.0.3
- **Documentation**: Swagger/OpenAPI
- **Architecture**: Clean Architecture with Service Layer Pattern

## 📁 Project Structure

```
GainIt/
├── GainIt.API/
│   ├── Controllers/          # API endpoints
│   │   ├── Projects/        # Project management endpoints
│   │   └── Users/           # User management endpoints
│   ├── Data/               # Database context and migrations
│   ├── DTOs/               # Data Transfer Objects
│   │   ├── Requests/       # Request models
│   │   └── ViewModels/     # Response models
│   ├── Models/             # Domain models
│   │   ├── Enums/         # Enumerations
│   │   ├── Projects/      # Project-related models
│   │   └── Users/         # User-related models
│   └── Services/           # Business logic layer
│       ├── Projects/       # Project services
│       └── Users/         # User services
```

## 🚀 Getting Started

### Prerequisites

- .NET 8 SDK
- PostgreSQL database
- Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/GainIt.git
   cd GainIt
   ```

2. **Configure the database**
   - Update the connection string in `appsettings.json`
   - Run Entity Framework migrations:
   ```bash
   cd GainIt.API
   dotnet ef database update
   ```

3. **Run the application**
   ```bash
   dotnet run
   ```

4. **Access the API**
   - API Base URL: `https://localhost:7001`
   - Swagger Documentation: `https://localhost:7001/swagger`

## 📚 API Endpoints

### Users
- `GET /api/users/gainer/{id}/profile` - Get Gainer profile
- `GET /api/users/mentor/{id}/profile` - Get Mentor profile  
- `GET /api/users/nonprofit/{id}/profile` - Get Nonprofit profile

### Projects
- `GET /api/projects/{projectId}` - Get active project
- `GET /api/projects/templates` - Get all template projects
- `GET /api/projects/nonprofits` - Get nonprofit projects
- `GET /api/projects/active` - Get all active projects
- `POST /api/projects/start-from-template` - Create project from template
- `PUT /api/projects/{projectId}/mentor` - Assign mentor to project
- `POST /api/projects/{projectId}/team-members` - Add team member
- `PUT /api/projects/{projectId}/status` - Update project status
- `GET /api/projects/search` - Search projects
- `GET /api/projects/filter` - Filter projects by criteria

## 🗄 Database Schema

The application uses PostgreSQL with the following key entities:

- **Users**: Base user information and profiles
- **Gainers**: Developers seeking experience
- **Mentors**: Experienced developers providing guidance
- **NonprofitOrganizations**: Organizations needing software solutions
- **UserProjects**: Active projects with team members
- **TemplateProjects**: Project templates for quick start
- **ProjectMembers**: Team member relationships
- **UserAchievements**: Achievement tracking system

## 🔧 Development

### Adding New Features
1. Create models in the appropriate `Models/` directory
2. Add DTOs in `DTOs/` for API contracts
3. Implement services in `Services/` for business logic
4. Create controllers in `Controllers/` for API endpoints
5. Update database with migrations

### Code Style
- Follow C# naming conventions
- Use async/await for database operations
- Implement proper error handling
- Add XML documentation for public APIs

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
- Swagger for API documentation

---

**GainIt** - Connecting developers, mentors, and nonprofits for meaningful software projects! 🚀
