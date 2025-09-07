# Frontend Flow Diagrams

This document contains visual flow diagrams for GitHub integration frontend implementation.

## 1. Complete User Journey Flow

```mermaid
graph TD
    A[User opens Project Dashboard] --> B{Repository Linked?}
    B -->|No| C[Show Link Repository Form]
    B -->|Yes| D[Load Project Overview]
    
    C --> E[User enters GitHub URL]
    E --> F[Validate URL]
    F --> G{Valid?}
    G -->|No| H[Show Error Message]
    G -->|Yes| I[Link Repository]
    I --> J{Success?}
    J -->|No| K[Show Link Error]
    J -->|Yes| L[Show Success Message]
    L --> D
    
    D --> M[Display Repository Info]
    D --> N[Display Statistics]
    D --> O[Display Analytics]
    D --> P[Display Contributions]
    D --> Q[Display Activity Summary]
    
    M --> R[User clicks View Details]
    R --> S[Load Detailed Analytics]
    S --> T[Show Charts & Graphs]
    
    N --> U[User clicks Sync Data]
    U --> V[Start Sync Process]
    V --> W[Show Progress]
    W --> X[Refresh Dashboard]
    
    H --> E
    K --> E
```

## 2. API Call Flow

```mermaid
sequenceDiagram
    participant U as User
    participant F as Frontend
    participant A as API
    participant G as GitHub Service
    
    U->>F: Opens Project Dashboard
    F->>A: GET /projects/{id}/overview
    A->>G: Get Repository Data
    A->>G: Get Statistics
    A->>G: Get Analytics
    A->>G: Get Contributions
    A->>G: Get Activity Summary
    A->>G: Get Sync Status
    G-->>A: All Data (Parallel)
    A-->>F: Comprehensive Overview
    F-->>U: Display Dashboard
    
    U->>F: Clicks "View Analytics"
    F->>A: GET /projects/{id}/analytics?force=true
    A->>G: Refresh Analytics
    G-->>A: Fresh Analytics Data
    A-->>F: Updated Analytics
    F-->>U: Show Detailed Charts
    
    U->>F: Clicks "Sync Data"
    F->>A: POST /projects/{id}/sync
    A->>G: Start Sync Process
    A-->>F: Sync Started
    F->>A: GET /projects/{id}/sync-status
    A-->>F: Sync Progress
    F-->>U: Show Progress Bar
```

## 3. Component State Flow

```mermaid
stateDiagram-v2
    [*] --> Initial
    Initial --> Loading: User opens dashboard
    Loading --> Success: API call succeeds
    Loading --> Error: API call fails
    Success --> Refreshing: User clicks refresh
    Refreshing --> Success: Refresh succeeds
    Refreshing --> Error: Refresh fails
    Error --> Loading: User retries
    Success --> Syncing: User clicks sync
    Syncing --> Success: Sync completes
    Syncing --> Error: Sync fails
    Error --> [*]: User closes
    Success --> [*]: User closes
```

## 4. Data Loading Strategy

```mermaid
graph TD
    A[Page Load] --> B[Check Cache]
    B --> C{Cache Valid?}
    C -->|Yes| D[Use Cached Data]
    C -->|No| E[Load Fresh Data]
    
    E --> F[Call Overview Endpoint]
    F --> G[Display Data]
    G --> H[Cache Data]
    
    D --> I[Display Cached Data]
    I --> J[Background Refresh]
    J --> K[Update Cache]
    K --> L[Update UI]
    
    M[User Action] --> N[Specific Endpoint Call]
    N --> O[Update Specific Data]
    O --> P[Refresh UI Component]
```

## 5. Error Handling Flow

```mermaid
graph TD
    A[API Call] --> B{Response Status}
    B -->|200| C[Success - Update UI]
    B -->|400| D[Bad Request]
    B -->|404| E[Not Found]
    B -->|500| F[Server Error]
    B -->|Network Error| G[Connection Issue]
    
    D --> H[Show Validation Error]
    E --> I[Show Not Found Message]
    F --> J[Show Server Error]
    G --> K[Show Network Error]
    
    H --> L[Allow User to Retry]
    I --> M[Show Link Repository Prompt]
    J --> N[Show Retry Button]
    K --> O[Show Offline Indicator]
    
    L --> A
    M --> P[Go to Link Flow]
    N --> A
    O --> Q[Wait for Connection]
    Q --> A
```

## 6. Mobile Responsive Flow

```mermaid
graph TD
    A[Mobile View] --> B[Collapse Heavy Sections]
    B --> C[Show Key Metrics Only]
    C --> D[User Scrolls Down]
    D --> E[Load More Data]
    E --> F[Show Additional Sections]
    
    G[Tablet View] --> H[Show 2-Column Layout]
    H --> I[Display Charts Side by Side]
    
    J[Desktop View] --> K[Show Full Dashboard]
    K --> L[Display All Sections]
    L --> M[Show Detailed Analytics]
```

## 7. Performance Optimization Flow

```mermaid
graph TD
    A[User Interaction] --> B{Data Cached?}
    B -->|Yes| C[Use Cached Data]
    B -->|No| D[Check Loading State]
    
    D --> E{Already Loading?}
    E -->|Yes| F[Wait for Current Request]
    E -->|No| G[Start New Request]
    
    G --> H[Show Loading Indicator]
    H --> I[Make API Call]
    I --> J[Cache Response]
    J --> K[Update UI]
    
    C --> L[Show Cached Data]
    L --> M[Background Refresh]
    M --> N[Update Cache Silently]
    
    F --> O[Show Loading Indicator]
    O --> P[Wait for Response]
    P --> K
```

## 8. Sync Process Flow

```mermaid
graph TD
    A[User Clicks Sync] --> B[Show Sync Modal]
    B --> C[Call Sync API]
    C --> D[Start Progress Polling]
    
    D --> E[Check Sync Status]
    E --> F{Status}
    F -->|In Progress| G[Update Progress Bar]
    F -->|Completed| H[Show Success Message]
    F -->|Failed| I[Show Error Message]
    
    G --> J[Wait 2 seconds]
    J --> E
    
    H --> K[Refresh Dashboard]
    I --> L[Show Retry Button]
    L --> C
    
    K --> M[Close Modal]
    M --> N[Show Updated Data]
```

## 9. Component Hierarchy

```mermaid
graph TD
    A[Project Dashboard] --> B[Repository Info Card]
    A --> C[Statistics Cards]
    A --> D[Analytics Section]
    A --> E[Contributions Table]
    A --> F[Activity Summary]
    A --> G[Sync Status]
    
    B --> H[Repository Name]
    B --> I[Owner Info]
    B --> J[Description]
    B --> K[Stars/Forks]
    
    C --> L[Total Commits]
    C --> M[Active Contributors]
    C --> N[Open Issues]
    C --> O[Pull Requests]
    
    D --> P[Commits Chart]
    D --> Q[Language Distribution]
    D --> R[Contributor Activity]
    D --> S[Issues Timeline]
    
    E --> T[User List]
    E --> U[Contribution Stats]
    E --> V[Activity Indicators]
    
    F --> W[AI Summary Text]
    F --> X[Key Insights]
    
    G --> Y[Sync Progress]
    G --> Z[Last Sync Time]
    G --> AA[Error Messages]
```

## 10. Data Flow Architecture

```mermaid
graph LR
    A[GitHub API] --> B[GitHub Service]
    B --> C[GitHub Controller]
    C --> D[Frontend API Client]
    D --> E[State Management]
    E --> F[UI Components]
    F --> G[User Interface]
    
    H[Cache Layer] --> D
    I[Error Handler] --> D
    J[Loading Manager] --> D
    K[Sync Manager] --> D
    
    L[Local Storage] --> E
    M[Session Storage] --> E
```

These diagrams provide a comprehensive visual guide for frontend developers to understand the complete flow of GitHub integration, from user interactions to API calls and state management.
