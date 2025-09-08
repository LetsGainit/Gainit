### GainIt Backend Documentation

This folder centralizes backend documentation for the ASP.NET Core API. It mirrors the codebase structure to make it easy to find explanations next to where code logically lives.

- See `Controllers/` for API endpoints and usage
- See `Services/` for business logic and integrations
- See `Data/` for EF Core data access and seeds
- See `Models/` for domain entities and enums
- See `DTOs/` for request/response contracts and view models
- See `Middleware/`, `Options/`, `Realtime/`, `HealthChecks/`, `Migrations/` for cross-cutting concerns

Existing detailed docs:
- [API Endpoints](./API/API_ENDPOINTS_DOCUMENTATION.md)
- [User Dashboard Endpoints](./Controllers/USER_DASHBOARD_ENDPOINT_DOCUMENTATION.md)
- [User Profile Update API](./Controllers/USER_PROFILE_UPDATE_API.md)
- [GitHub Integration Overview](./Services/GitHub/GITHUB_INTEGRATION.md)
- [GitHub API Reference Card](./Services/GitHub/API_REFERENCE_CARD.md)
- [Vector Search Frontend Integration](./Services/VECTOR_SEARCH_FRONTEND_INTEGRATION.md)
- [SignalR Frontend Integration](./Realtime/SIGNALR_FRONTEND_INTEGRATION_GUIDE.md)
- [DB Connection String](./Data/DB/CONNECTION_STRING.md)
- [RAG Indexer Configuration](./Data/DB/02_RAG_IMPROVEMENT_INDEXER_CONFIGURATION.md)

Subfolders
- [Controllers](./Controllers/README.md)
- [Services](./Services/README.md)
- [Data](./Data/README.md)
- [Models](./Models/README.md)
- [DTOs](./DTOs/README.md)
- [Middleware](./Middleware/README.md)
- [Options](./Options/README.md)
- [Realtime](./Realtime/README.md)
- [HealthChecks](./HealthChecks/README.md)
- [Migrations](./Migrations/README.md)
- [API (Existing Docs Index)](./API/README.md)

Diagrams
- [Architecture Overview](./architecture.md)

Conventions
- Keep detailed docs here; keep top-level `README.md` concise for onboarding
- Use relative links between docs
- Prefer code samples and short diagrams for clarity


