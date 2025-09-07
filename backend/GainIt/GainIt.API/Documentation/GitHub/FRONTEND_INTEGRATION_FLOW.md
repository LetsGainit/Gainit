# GitHub Integration Frontend Flow Guide

This document outlines the complete flow for frontend integration with the GitHub controller endpoints, including user journeys, API interactions, and UI state management.

## 🎯 Overview

The GitHub integration provides comprehensive repository management, analytics, and contribution tracking. The frontend can interact with 13 different endpoints to provide a rich GitHub experience.

## 📋 Available Endpoints

| Endpoint | Method | Purpose | Key Features |
|----------|--------|---------|--------------|
| `/projects/{projectId}/link` | POST | Link repository | URL validation, conflict handling |
| `/projects/{projectId}/repository` | GET | Get repository info | Basic repository details |
| `/projects/{projectId}/stats` | GET | Get repository stats | Stars, forks, issues, contributors |
| `/projects/{projectId}/analytics` | GET | Get project analytics | Commits, issues, PRs, contributors |
| `/projects/{projectId}/contributions` | GET | Get user contributions | All project members' contributions |
| `/projects/{projectId}/users/{userId}/contributions` | GET | Get user contribution details | Detailed individual user stats |
| `/projects/{projectId}/users/{userId}/activity` | GET | Get user activity summary | AI-generated user activity summary |
| `/projects/{projectId}/activity-summary` | GET | Get project activity summary | AI-generated project summary |
| `/projects/{projectId}/insights` | GET | Get personalized insights | AI-powered custom insights |
| `/projects/{projectId}/overview` | GET | Get comprehensive overview | **All data in one call** |
| `/projects/{projectId}/sync` | POST | Sync GitHub data | Manual data synchronization |
| `/projects/{projectId}/sync-status` | GET | Get sync status | Check sync progress |
| `/validate-url` | POST | Validate repository URL | Pre-link validation |
| `/repositories/{owner}/{name}/project` | GET | Resolve repository to project | Find linked project |

## 🚀 Frontend Integration Flows

### 1. Repository Linking Flow

```mermaid
graph TD
    A[User wants to link repository] --> B[Show URL input form]
    B --> C[User enters GitHub URL]
    C --> D[Call /validate-url endpoint]
    D --> E{URL Valid?}
    E -->|No| F[Show validation error]
    E -->|Yes| G[Call /projects/{projectId}/link]
    G --> H{Link Successful?}
    H -->|No| I[Show error message]
    H -->|Yes| J[Show success message]
    J --> K[Redirect to project overview]
    F --> C
    I --> C
```

**Frontend Implementation:**
```typescript
// 1. Validate URL first
const validateUrl = async (url: string) => {
  const response = await fetch('/api/github/validate-url', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ repositoryUrl: url })
  });
  return response.json();
};

// 2. Link repository
const linkRepository = async (projectId: string, url: string) => {
  const response = await fetch(`/api/github/projects/${projectId}/link`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ repositoryUrl: url })
  });
  return response.json();
};
```

### 2. Project Dashboard Flow

```mermaid
graph TD
    A[User opens project dashboard] --> B[Call /projects/{projectId}/overview]
    B --> C{Repository Linked?}
    C -->|No| D[Show "Link Repository" prompt]
    C -->|Yes| E[Display comprehensive data]
    E --> F[Repository Info Card]
    E --> G[Statistics Cards]
    E --> H[Analytics Charts]
    E --> I[Contributions Table]
    E --> J[Activity Summary]
    E --> K[Sync Status]
    D --> L[User clicks "Link Repository"]
    L --> M[Go to Repository Linking Flow]
```

**Frontend Implementation:**
```typescript
// Get comprehensive project overview
const getProjectOverview = async (projectId: string, daysPeriod: number = 30) => {
  const response = await fetch(`/api/github/projects/${projectId}/overview?daysPeriod=${daysPeriod}`);
  return response.json();
};

// Component state management
const [projectData, setProjectData] = useState({
  repository: null,
  stats: null,
  analytics: null,
  contributions: [],
  activitySummary: '',
  syncStatus: null
});
```

### 3. Analytics Deep Dive Flow

```mermaid
graph TD
    A[User wants detailed analytics] --> B[Call /projects/{projectId}/analytics]
    B --> C[Display analytics dashboard]
    C --> D[Commits over time chart]
    C --> E[Language distribution]
    C --> F[Contributor activity]
    C --> G[Issues and PRs timeline]
    G --> H[User clicks on specific data point]
    H --> I[Call detailed endpoint]
    I --> J[Show detailed view]
```

**Frontend Implementation:**
```typescript
// Get detailed analytics
const getAnalytics = async (projectId: string, daysPeriod: number = 30, force: boolean = false) => {
  const response = await fetch(`/api/github/projects/${projectId}/analytics?daysPeriod=${daysPeriod}&force=${force}`);
  return response.json();
};

// Get user-specific contributions
const getUserContributions = async (projectId: string, userId: string, daysPeriod: number = 30) => {
  const response = await fetch(`/api/github/projects/${projectId}/users/${userId}/contributions?daysPeriod=${daysPeriod}`);
  return response.json();
};
```

### 4. Data Synchronization Flow

```mermaid
graph TD
    A[User wants fresh data] --> B[Call /projects/{projectId}/sync]
    B --> C[Show loading spinner]
    C --> D[Call /projects/{projectId}/sync-status]
    D --> E{Sync Complete?}
    E -->|No| F[Update progress bar]
    F --> G[Wait 2 seconds]
    G --> D
    E -->|Yes| H[Refresh dashboard data]
    H --> I[Show success message]
```

**Frontend Implementation:**
```typescript
// Start sync process
const syncData = async (projectId: string, syncType: string = 'all') => {
  const response = await fetch(`/api/github/projects/${projectId}/sync?syncType=${syncType}`, {
    method: 'POST'
  });
  return response.json();
};

// Poll sync status
const pollSyncStatus = async (projectId: string) => {
  const response = await fetch(`/api/github/projects/${projectId}/sync-status`);
  return response.json();
};

// Sync with progress tracking
const syncWithProgress = async (projectId: string) => {
  await syncData(projectId);
  
  const pollInterval = setInterval(async () => {
    const status = await pollSyncStatus(projectId);
    updateProgressBar(status);
    
    if (status.syncStatus.status === 'completed') {
      clearInterval(pollInterval);
      refreshDashboard();
    }
  }, 2000);
};
```

## 🎨 UI Component Recommendations

### 1. Repository Link Component
```typescript
interface RepositoryLinkProps {
  projectId: string;
  onLinked: (repository: GitHubRepositoryInfoDto) => void;
}

const RepositoryLinkComponent = ({ projectId, onLinked }: RepositoryLinkProps) => {
  const [url, setUrl] = useState('');
  const [isValidating, setIsValidating] = useState(false);
  const [isLinking, setIsLinking] = useState(false);
  
  const handleLink = async () => {
    setIsValidating(true);
    const validation = await validateUrl(url);
    
    if (validation.isValid) {
      setIsLinking(true);
      const result = await linkRepository(projectId, url);
      onLinked(result.repository);
    }
  };
  
  return (
    <div className="repository-link">
      <input 
        type="url" 
        value={url} 
        onChange={(e) => setUrl(e.target.value)}
        placeholder="https://github.com/owner/repository"
      />
      <button onClick={handleLink} disabled={isValidating || isLinking}>
        {isLinking ? 'Linking...' : 'Link Repository'}
      </button>
    </div>
  );
};
```

### 2. Project Overview Dashboard
```typescript
interface ProjectOverviewProps {
  projectId: string;
  daysPeriod: number;
}

const ProjectOverviewDashboard = ({ projectId, daysPeriod }: ProjectOverviewProps) => {
  const [overview, setOverview] = useState<GitHubProjectOverviewResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  
  useEffect(() => {
    const loadOverview = async () => {
      try {
        const data = await getProjectOverview(projectId, daysPeriod);
        setOverview(data);
      } catch (error) {
        console.error('Failed to load overview:', error);
      } finally {
        setLoading(false);
      }
    };
    
    loadOverview();
  }, [projectId, daysPeriod]);
  
  if (loading) return <LoadingSpinner />;
  if (!overview) return <NoDataMessage />;
  
  return (
    <div className="project-overview">
      <RepositoryInfoCard repository={overview.repository} />
      <StatisticsCards stats={overview.stats} />
      <AnalyticsCharts analytics={overview.analytics} />
      <ContributionsTable contributions={overview.contributions} />
      <ActivitySummary summary={overview.activitySummary} />
      <SyncStatus status={overview.syncStatus} />
    </div>
  );
};
```

### 3. Analytics Dashboard
```typescript
const AnalyticsDashboard = ({ projectId, daysPeriod }: ProjectOverviewProps) => {
  const [analytics, setAnalytics] = useState<GitHubProjectAnalyticsResponseDto | null>(null);
  const [contributions, setContributions] = useState<GitHubUserContributionsResponseDto | null>(null);
  
  useEffect(() => {
    const loadAnalytics = async () => {
      const [analyticsData, contributionsData] = await Promise.all([
        getAnalytics(projectId, daysPeriod),
        getUserContributions(projectId, daysPeriod)
      ]);
      
      setAnalytics(analyticsData);
      setContributions(contributionsData);
    };
    
    loadAnalytics();
  }, [projectId, daysPeriod]);
  
  return (
    <div className="analytics-dashboard">
      <CommitsChart data={analytics?.analytics.weeklyCommits} />
      <LanguageDistribution data={analytics?.analytics.languageStats} />
      <ContributorsChart data={contributions?.contributions} />
      <IssuesTimeline data={analytics?.analytics} />
    </div>
  );
};
```

## 🔄 State Management Patterns

### 1. Loading States
```typescript
interface GitHubState {
  repository: GitHubRepositoryInfoDto | null;
  stats: GitHubRepositoryStatsDto | null;
  analytics: GitHubProjectAnalyticsResponseDto | null;
  contributions: GitHubUserContributionsResponseDto | null;
  overview: GitHubProjectOverviewResponseDto | null;
  loading: {
    repository: boolean;
    stats: boolean;
    analytics: boolean;
    contributions: boolean;
    overview: boolean;
    sync: boolean;
  };
  error: string | null;
}
```

### 2. Error Handling
```typescript
const handleApiError = (error: any, endpoint: string) => {
  console.error(`Error in ${endpoint}:`, error);
  
  if (error.status === 404) {
    return 'Data not found. Please check if the repository is linked.';
  } else if (error.status === 400) {
    return 'Invalid request. Please check your parameters.';
  } else if (error.status === 500) {
    return 'Server error. Please try again later.';
  }
  
  return 'An unexpected error occurred.';
};
```

### 3. Caching Strategy
```typescript
// Cache data for 5 minutes
const CACHE_DURATION = 5 * 60 * 1000;
const cache = new Map<string, { data: any; timestamp: number }>();

const getCachedData = async (key: string, fetchFn: () => Promise<any>) => {
  const cached = cache.get(key);
  const now = Date.now();
  
  if (cached && (now - cached.timestamp) < CACHE_DURATION) {
    return cached.data;
  }
  
  const data = await fetchFn();
  cache.set(key, { data, timestamp: now });
  return data;
};
```

## 📱 Mobile Considerations

### 1. Responsive Design
- Use collapsible sections for mobile
- Prioritize key metrics on small screens
- Implement swipe gestures for charts

### 2. Performance Optimization
- Lazy load heavy components
- Use virtual scrolling for large contribution lists
- Implement progressive loading for overview data

## 🚨 Error Scenarios

### 1. Repository Not Linked
- Show "Link Repository" prompt
- Provide clear call-to-action
- Guide user through linking process

### 2. Sync Failures
- Show retry button
- Display error message
- Allow manual sync trigger

### 3. Network Issues
- Implement retry logic
- Show offline indicator
- Cache data for offline viewing

## 🔧 Development Tips

### 1. API Testing
```typescript
// Test all endpoints with different parameters
const testEndpoints = async (projectId: string) => {
  const endpoints = [
    () => getProjectOverview(projectId, 7),
    () => getProjectOverview(projectId, 30),
    () => getProjectOverview(projectId, 90),
    () => getAnalytics(projectId, 30, true),
    () => getUserContributions(projectId, 30, true)
  ];
  
  for (const endpoint of endpoints) {
    try {
      const result = await endpoint();
      console.log('Success:', result);
    } catch (error) {
      console.error('Error:', error);
    }
  }
};
```

### 2. Performance Monitoring
```typescript
const measureApiCall = async (name: string, apiCall: () => Promise<any>) => {
  const start = performance.now();
  try {
    const result = await apiCall();
    const duration = performance.now() - start;
    console.log(`${name} took ${duration}ms`);
    return result;
  } catch (error) {
    const duration = performance.now() - start;
    console.error(`${name} failed after ${duration}ms:`, error);
    throw error;
  }
};
```

This flow guide provides everything the frontend team needs to successfully integrate with the GitHub controller endpoints. The comprehensive overview endpoint (`/overview`) is particularly powerful as it provides all data in a single call, making it perfect for dashboard implementations.
