### Architecture Overview

```mermaid
flowchart TB
  Client[Frontend / API Consumers]

  subgraph API[ASP.NET Core Web API]
    Controllers[Controllers]
    Services[Services]
    Middleware[Middleware]
    Realtime[SignalR Hubs]
    Health[Health Checks]
  end

  subgraph Domain[Domain Models & Contracts]
    Models[Domain Models]
    DTOs[DTOs & ViewModels]
    Enums[Enums]
  end

  subgraph Data[Data & Persistence]
    DbContext[EF Core DbContext]
    Database[(PostgreSQL)]
    Migrations[Migrations]
  end

  subgraph External[External Integrations]
    GitHub[GitHub REST API]
    AzureEmail[Azure Communication Services]
    Blob[Azure Blob Storage]
    Search[Azure Cognitive Search]
  end

  Client --> Controllers
  Controllers --> Services
  Controllers --> Middleware
  Controllers --> DTOs
  Services --> Models
  Services --> DbContext
  Services --> Realtime
  Services --> GitHub
  Services --> AzureEmail
  Services --> Blob
  Services --> Search
  DbContext --> Database
```

Legend
- Controllers: HTTP endpoints mapping requests to services
- Services: Business logic and integration adapters
- DbContext: EF Core data access to PostgreSQL
- Models/DTOs/Enums: Domain entities and API contracts
- Middleware: Cross-cutting concerns
- Realtime: SignalR notifications
- External: GitHub and Azure services

 
