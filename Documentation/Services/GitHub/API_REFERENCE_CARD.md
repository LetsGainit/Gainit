# GitHub API Reference Card



Quick reference for frontend developers integrating with the GitHub controller.

## 🚀 Essential Endpoints

### 1. Get Comprehensive Overview (Recommended)
```http
GET /api/github/projects/{projectId}/overview?daysPeriod=30
```
**Use this for:** Main dashboard, complete project view
**Returns:** All GitHub data in one call (repository, stats, analytics, contributions, activity summary, sync status)


## 📊 Data-Specific Endpoints

### Repository Information
```http
GET /api/github/projects/{projectId}/repository
```
**Returns:** Basic repository info (name, owner, description, stars, forks)

### Repository Statistics
```http
GET /api/github/projects/{projectId}/stats
```
**Returns:** Detailed stats (issues, PRs, branches, contributors, top contributors)

### Project Analytics
```http
GET /api/github/projects/{projectId}/analytics?daysPeriod=30&force=false
```
**Returns:** Commits, issues, PRs, language stats, contributor activity

### User Contributions
```http
GET /api/github/projects/{projectId}/contributions?daysPeriod=30&force=false
```
**Returns:** All project members' contribution data

### Individual User Contribution
```http
GET /api/github/projects/{projectId}/users/{userId}/contributions?daysPeriod=30&force=false
```
**Returns:** Detailed contribution data for specific user

## 🤖 AI-Powered Endpoints

### Project Activity Summary
```http
GET /api/github/projects/{projectId}/activity-summary?daysPeriod=30
```
**Returns:** AI-generated project activity summary

### User Activity Summary
```http
GET /api/github/projects/{projectId}/users/{userId}/activity?daysPeriod=30
```
**Returns:** AI-generated user activity summary

### Personalized Insights
```http
GET /api/github/projects/{projectId}/insights?userQuery=What are the most active contributors?&daysPeriod=30
```
**Returns:** AI-powered custom insights based on user query

## 🔄 Data Management

### Sync Data
```http
POST /api/github/projects/{projectId}/sync?syncType=all
```
**Sync Types:** `repository`, `analytics`, `all`
**Returns:** Sync operation result

### Get Sync Status
```http
GET /api/github/projects/{projectId}/sync-status
```
**Returns:** Current sync progress and status

### Resolve Repository to Project
```http
GET /api/github/repositories/{owner}/{name}/project
```
**Returns:** Project ID if repository is linked

## 📝 Response Codes

| Code | Meaning | Action |
|------|---------|--------|
| 200 | Success | Use the data |
| 400 | Bad Request | Check parameters, show validation error |
| 404 | Not Found | Show "not found" message, suggest linking repository |
| 409 | Conflict | Repository already linked |
| 500 | Server Error | Show error message, offer retry |

## 🎯 Frontend Implementation Tips

### 1. Start with Overview Endpoint
```typescript
// Get everything in one call
const overview = await fetch(`/api/github/projects/${projectId}/overview?daysPeriod=30`);
const data = await overview.json();

// Use data.repository, data.stats, data.analytics, etc.
```

### 2. Handle Loading States
```typescript
const [loading, setLoading] = useState(true);
const [error, setError] = useState(null);
const [data, setData] = useState(null);

try {
  setLoading(true);
  const response = await fetch(url);
  if (!response.ok) throw new Error(`HTTP ${response.status}`);
  setData(await response.json());
} catch (err) {
  setError(err.message);
} finally {
  setLoading(false);
}
```

### 3. Implement Caching
```typescript
const CACHE_DURATION = 5 * 60 * 1000; // 5 minutes
const cache = new Map();

const getCachedData = async (key, fetchFn) => {
  const cached = cache.get(key);
  if (cached && (Date.now() - cached.timestamp) < CACHE_DURATION) {
    return cached.data;
  }
  
  const data = await fetchFn();
  cache.set(key, { data, timestamp: Date.now() });
  return data;
};
```

### 4. Sync with Progress
```typescript
const syncWithProgress = async (projectId) => {
  // Start sync
  await fetch(`/api/github/projects/${projectId}/sync`, { method: 'POST' });
  
  // Poll for completion
  const pollInterval = setInterval(async () => {
    const status = await fetch(`/api/github/projects/${projectId}/sync-status`);
    const data = await status.json();
    
    updateProgressBar(data.syncStatus);
    
    if (data.syncStatus.status === 'completed') {
      clearInterval(pollInterval);
      refreshDashboard();
    }
  }, 2000);
};
```

## 🔧 Common Patterns

### Error Handling
```typescript
const handleApiError = (error, endpoint) => {
  if (error.status === 404) return 'Repository not linked. Please link a repository first.';
  if (error.status === 400) return 'Invalid request. Please check your parameters.';
  if (error.status === 500) return 'Server error. Please try again later.';
  return 'An unexpected error occurred.';
};
```

### Parameter Validation
```typescript
const validateDaysPeriod = (days) => {
  return days >= 1 && days <= 365;
};

const validateProjectId = (id) => {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(id);
};
```

### URL Validation
```typescript
const validateGitHubUrl = (url) => {
  const pattern = /^https:\/\/github\.com\/[a-zA-Z0-9._-]+\/[a-zA-Z0-9._-]+$/;
  return pattern.test(url);
};
```

## 📱 Mobile Considerations

- Use the overview endpoint to minimize API calls
- Implement progressive loading for large datasets
- Cache data locally for offline viewing
- Use collapsible sections for better mobile UX

## 🚨 Common Issues

1. **Repository not linked**: Use the link endpoint first
2. **Stale data**: Use `force=true` parameter or sync data
3. **Large datasets**: Implement pagination or virtual scrolling
4. **Network errors**: Implement retry logic with exponential backoff

## 💡 Best Practices

1. **Always validate URLs** before linking
2. **Use the overview endpoint** for dashboards
3. **Implement proper loading states** for better UX
4. **Cache data** to reduce API calls
5. **Handle errors gracefully** with user-friendly messages
6. **Use TypeScript** for better type safety
7. **Implement responsive design** for all screen sizes
