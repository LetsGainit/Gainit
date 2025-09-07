# Frontend Implementation Guide

This guide provides practical implementation examples for integrating with the GitHub controller endpoints.

## 🚀 Quick Start

### 1. API Client Setup

```typescript
// api/github.ts
class GitHubApiClient {
  private baseUrl = '/api/github';
  
  async getProjectOverview(projectId: string, daysPeriod: number = 30): Promise<GitHubProjectOverviewResponseDto> {
    const response = await fetch(`${this.baseUrl}/projects/${projectId}/overview?daysPeriod=${daysPeriod}`);
    if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    return response.json();
  }
  
  async linkRepository(projectId: string, repositoryUrl: string): Promise<GitHubRepositoryLinkResponseDto> {
    const response = await fetch(`${this.baseUrl}/projects/${projectId}/link`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ repositoryUrl })
    });
    if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    return response.json();
  }
  
  async validateUrl(repositoryUrl: string): Promise<UrlValidationResponseDto> {
    const response = await fetch(`${this.baseUrl}/validate-url`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ repositoryUrl })
    });
    if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    return response.json();
  }
  
  async syncData(projectId: string, syncType: string = 'all'): Promise<GitHubSyncResponseDto> {
    const response = await fetch(`${this.baseUrl}/projects/${projectId}/sync?syncType=${syncType}`, {
      method: 'POST'
    });
    if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    return response.json();
  }
  
  async getSyncStatus(projectId: string): Promise<SyncStatusResponseDto> {
    const response = await fetch(`${this.baseUrl}/projects/${projectId}/sync-status`);
    if (!response.ok) throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    return response.json();
  }
}

export const githubApi = new GitHubApiClient();
```

### 2. TypeScript Interfaces

```typescript
// types/github.ts
export interface GitHubProjectOverviewResponseDto {
  projectId: string;
  daysPeriod: number;
  generatedAt: string;
  repository: GitHubRepositoryOverviewDto;
  stats: GitHubRepositoryStatsOverviewDto | null;
  analytics: GitHubAnalyticsOverviewDto | null;
  contributions: GitHubContributionOverviewDto[];
  activitySummary: string;
  syncStatus: GitHubSyncStatusOverviewDto | null;
}

export interface GitHubRepositoryOverviewDto {
  repositoryId: string;
  repositoryName: string;
  ownerName: string;
  fullName: string;
  description: string | null;
  isPublic: boolean;
  primaryLanguage: string | null;
  languages: string[];
  starsCount: number;
  forksCount: number;
  openIssuesCount: number;
  openPullRequestsCount: number;
  defaultBranch: string | null;
  lastActivityAtUtc: string;
  lastSyncedAtUtc: string;
  branches: string[];
}

export interface GitHubRepositoryStatsOverviewDto {
  starsCount: number;
  forksCount: number;
  issueCount: number;
  pullRequestCount: number;
  branchCount: number;
  releaseCount: number;
  contributors: number;
  topContributors: TopContributorOverviewDto[];
}

export interface GitHubContributionOverviewDto {
  userId: string;
  githubUsername: string | null;
  totalCommits: number;
  totalLinesChanged: number;
  totalIssuesCreated: number;
  totalPullRequestsCreated: number;
  totalReviews: number;
  uniqueDaysWithCommits: number;
  filesModified: number;
  languagesContributed: string[];
  longestStreak: number;
  currentStreak: number;
  calculatedAtUtc: string;
}

// ... other interfaces
```

## 🎨 React Components

### 1. Main Dashboard Component

```tsx
// components/GitHubDashboard.tsx
import React, { useState, useEffect } from 'react';
import { githubApi } from '../api/github';
import { GitHubProjectOverviewResponseDto } from '../types/github';
import { RepositoryInfoCard } from './RepositoryInfoCard';
import { StatisticsCards } from './StatisticsCards';
import { AnalyticsCharts } from './AnalyticsCharts';
import { ContributionsTable } from './ContributionsTable';
import { ActivitySummary } from './ActivitySummary';
import { SyncStatus } from './SyncStatus';
import { LoadingSpinner } from './LoadingSpinner';
import { ErrorMessage } from './ErrorMessage';

interface GitHubDashboardProps {
  projectId: string;
  daysPeriod?: number;
}

export const GitHubDashboard: React.FC<GitHubDashboardProps> = ({ 
  projectId, 
  daysPeriod = 30 
}) => {
  const [overview, setOverview] = useState<GitHubProjectOverviewResponseDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const loadOverview = async (showLoading = true) => {
    try {
      if (showLoading) setLoading(true);
      setError(null);
      
      const data = await githubApi.getProjectOverview(projectId, daysPeriod);
      setOverview(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load overview');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const handleRefresh = () => {
    setRefreshing(true);
    loadOverview(false);
  };

  const handleSync = async () => {
    try {
      await githubApi.syncData(projectId);
      // Poll for sync completion
      const pollInterval = setInterval(async () => {
        const status = await githubApi.getSyncStatus(projectId);
        if (status.syncStatus.status === 'completed') {
          clearInterval(pollInterval);
          loadOverview(false);
        }
      }, 2000);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Sync failed');
    }
  };

  useEffect(() => {
    loadOverview();
  }, [projectId, daysPeriod]);

  if (loading) return <LoadingSpinner />;
  if (error) return <ErrorMessage message={error} onRetry={() => loadOverview()} />;
  if (!overview) return <ErrorMessage message="No data available" onRetry={() => loadOverview()} />;

  return (
    <div className="github-dashboard">
      <div className="dashboard-header">
        <h2>GitHub Analytics</h2>
        <div className="dashboard-actions">
          <button onClick={handleRefresh} disabled={refreshing}>
            {refreshing ? 'Refreshing...' : 'Refresh'}
          </button>
          <button onClick={handleSync}>
            Sync Data
          </button>
        </div>
      </div>

      <div className="dashboard-content">
        <RepositoryInfoCard repository={overview.repository} />
        <StatisticsCards stats={overview.stats} />
        <AnalyticsCharts analytics={overview.analytics} />
        <ContributionsTable contributions={overview.contributions} />
        <ActivitySummary summary={overview.activitySummary} />
        <SyncStatus status={overview.syncStatus} />
      </div>
    </div>
  );
};
```

### 2. Repository Info Card

```tsx
// components/RepositoryInfoCard.tsx
import React from 'react';
import { GitHubRepositoryOverviewDto } from '../types/github';

interface RepositoryInfoCardProps {
  repository: GitHubRepositoryOverviewDto;
}

export const RepositoryInfoCard: React.FC<RepositoryInfoCardProps> = ({ repository }) => {
  return (
    <div className="repository-info-card">
      <div className="repository-header">
        <h3>{repository.repositoryName}</h3>
        <span className="owner">by {repository.ownerName}</span>
      </div>
      
      <div className="repository-details">
        <p className="description">{repository.description || 'No description available'}</p>
        
        <div className="repository-stats">
          <div className="stat">
            <span className="stat-value">{repository.starsCount}</span>
            <span className="stat-label">Stars</span>
          </div>
          <div className="stat">
            <span className="stat-value">{repository.forksCount}</span>
            <span className="stat-label">Forks</span>
          </div>
          <div className="stat">
            <span className="stat-value">{repository.openIssuesCount}</span>
            <span className="stat-label">Issues</span>
          </div>
          <div className="stat">
            <span className="stat-value">{repository.openPullRequestsCount}</span>
            <span className="stat-label">PRs</span>
          </div>
        </div>
        
        <div className="repository-meta">
          <span className="language">{repository.primaryLanguage || 'Unknown'}</span>
          <span className="visibility">{repository.isPublic ? 'Public' : 'Private'}</span>
          <span className="last-activity">
            Last activity: {new Date(repository.lastActivityAtUtc).toLocaleDateString()}
          </span>
        </div>
      </div>
    </div>
  );
};
```

### 3. Statistics Cards

```tsx
// components/StatisticsCards.tsx
import React from 'react';
import { GitHubRepositoryStatsOverviewDto } from '../types/github';

interface StatisticsCardsProps {
  stats: GitHubRepositoryStatsOverviewDto | null;
}

export const StatisticsCards: React.FC<StatisticsCardsProps> = ({ stats }) => {
  if (!stats) return <div className="no-stats">No statistics available</div>;

  return (
    <div className="statistics-cards">
      <div className="stat-card">
        <h4>Repository Stats</h4>
        <div className="stats-grid">
          <div className="stat-item">
            <span className="stat-value">{stats.starsCount}</span>
            <span className="stat-label">Stars</span>
          </div>
          <div className="stat-item">
            <span className="stat-value">{stats.forksCount}</span>
            <span className="stat-label">Forks</span>
          </div>
          <div className="stat-item">
            <span className="stat-value">{stats.issueCount}</span>
            <span className="stat-label">Issues</span>
          </div>
          <div className="stat-item">
            <span className="stat-value">{stats.pullRequestCount}</span>
            <span className="stat-label">Pull Requests</span>
          </div>
          <div className="stat-item">
            <span className="stat-value">{stats.branchCount}</span>
            <span className="stat-label">Branches</span>
          </div>
          <div className="stat-item">
            <span className="stat-value">{stats.releaseCount}</span>
            <span className="stat-label">Releases</span>
          </div>
        </div>
      </div>
      
      <div className="stat-card">
        <h4>Contributors</h4>
        <div className="contributors-info">
          <span className="total-contributors">{stats.contributors} total contributors</span>
          {stats.topContributors.length > 0 && (
            <div className="top-contributors">
              <h5>Top Contributors</h5>
              <ul>
                {stats.topContributors.slice(0, 5).map((contributor, index) => (
                  <li key={index}>
                    <span className="username">{contributor.githubUsername}</span>
                    <span className="commits">{contributor.totalCommits} commits</span>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
```

### 4. Contributions Table

```tsx
// components/ContributionsTable.tsx
import React, { useState } from 'react';
import { GitHubContributionOverviewDto } from '../types/github';

interface ContributionsTableProps {
  contributions: GitHubContributionOverviewDto[];
}

export const ContributionsTable: React.FC<ContributionsTableProps> = ({ contributions }) => {
  const [sortBy, setSortBy] = useState<keyof GitHubContributionOverviewDto>('totalCommits');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');

  const sortedContributions = [...contributions].sort((a, b) => {
    const aValue = a[sortBy];
    const bValue = b[sortBy];
    
    if (typeof aValue === 'number' && typeof bValue === 'number') {
      return sortOrder === 'asc' ? aValue - bValue : bValue - aValue;
    }
    
    return sortOrder === 'asc' 
      ? String(aValue).localeCompare(String(bValue))
      : String(bValue).localeCompare(String(aValue));
  });

  const handleSort = (column: keyof GitHubContributionOverviewDto) => {
    if (sortBy === column) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc');
    } else {
      setSortBy(column);
      setSortOrder('desc');
    }
  };

  return (
    <div className="contributions-table">
      <h3>Contributions</h3>
      <div className="table-container">
        <table>
          <thead>
            <tr>
              <th onClick={() => handleSort('githubUsername')}>
                User {sortBy === 'githubUsername' && (sortOrder === 'asc' ? '↑' : '↓')}
              </th>
              <th onClick={() => handleSort('totalCommits')}>
                Commits {sortBy === 'totalCommits' && (sortOrder === 'asc' ? '↑' : '↓')}
              </th>
              <th onClick={() => handleSort('totalLinesChanged')}>
                Lines Changed {sortBy === 'totalLinesChanged' && (sortOrder === 'asc' ? '↑' : '↓')}
              </th>
              <th onClick={() => handleSort('totalIssuesCreated')}>
                Issues {sortBy === 'totalIssuesCreated' && (sortOrder === 'asc' ? '↑' : '↓')}
              </th>
              <th onClick={() => handleSort('totalPullRequestsCreated')}>
                PRs {sortBy === 'totalPullRequestsCreated' && (sortOrder === 'asc' ? '↑' : '↓')}
              </th>
              <th onClick={() => handleSort('currentStreak')}>
                Current Streak {sortBy === 'currentStreak' && (sortOrder === 'asc' ? '↑' : '↓')}
              </th>
            </tr>
          </thead>
          <tbody>
            {sortedContributions.map((contribution) => (
              <tr key={contribution.userId}>
                <td>{contribution.githubUsername || 'Unknown'}</td>
                <td>{contribution.totalCommits.toLocaleString()}</td>
                <td>{contribution.totalLinesChanged.toLocaleString()}</td>
                <td>{contribution.totalIssuesCreated}</td>
                <td>{contribution.totalPullRequestsCreated}</td>
                <td>{contribution.currentStreak} days</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
```

### 5. Repository Link Component

```tsx
// components/RepositoryLink.tsx
import React, { useState } from 'react';
import { githubApi } from '../api/github';

interface RepositoryLinkProps {
  projectId: string;
  onLinked: (repository: any) => void;
}

export const RepositoryLink: React.FC<RepositoryLinkProps> = ({ projectId, onLinked }) => {
  const [url, setUrl] = useState('');
  const [isValidating, setIsValidating] = useState(false);
  const [isLinking, setIsLinking] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [validationResult, setValidationResult] = useState<any>(null);

  const handleValidate = async () => {
    if (!url.trim()) return;
    
    setIsValidating(true);
    setError(null);
    
    try {
      const result = await githubApi.validateUrl(url);
      setValidationResult(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Validation failed');
    } finally {
      setIsValidating(false);
    }
  };

  const handleLink = async () => {
    if (!validationResult?.isValid) return;
    
    setIsLinking(true);
    setError(null);
    
    try {
      const result = await githubApi.linkRepository(projectId, url);
      onLinked(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Linking failed');
    } finally {
      setIsLinking(false);
    }
  };

  return (
    <div className="repository-link">
      <h3>Link GitHub Repository</h3>
      
      <div className="link-form">
        <input
          type="url"
          value={url}
          onChange={(e) => setUrl(e.target.value)}
          placeholder="https://github.com/owner/repository"
          disabled={isValidating || isLinking}
        />
        
        <button 
          onClick={handleValidate} 
          disabled={!url.trim() || isValidating || isLinking}
        >
          {isValidating ? 'Validating...' : 'Validate'}
        </button>
      </div>
      
      {validationResult && (
        <div className={`validation-result ${validationResult.isValid ? 'valid' : 'invalid'}`}>
          <p>{validationResult.message}</p>
          {validationResult.isValid && (
            <button onClick={handleLink} disabled={isLinking}>
              {isLinking ? 'Linking...' : 'Link Repository'}
            </button>
          )}
        </div>
      )}
      
      {error && (
        <div className="error-message">
          <p>{error}</p>
        </div>
      )}
    </div>
  );
};
```

## 🎯 State Management (Redux/Zustand)

### Redux Implementation

```typescript
// store/githubSlice.ts
import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { githubApi } from '../api/github';

interface GitHubState {
  overview: GitHubProjectOverviewResponseDto | null;
  loading: boolean;
  error: string | null;
  lastUpdated: number | null;
}

const initialState: GitHubState = {
  overview: null,
  loading: false,
  error: null,
  lastUpdated: null,
};

export const fetchProjectOverview = createAsyncThunk(
  'github/fetchOverview',
  async ({ projectId, daysPeriod }: { projectId: string; daysPeriod: number }) => {
    return await githubApi.getProjectOverview(projectId, daysPeriod);
  }
);

export const syncProjectData = createAsyncThunk(
  'github/syncData',
  async ({ projectId, syncType }: { projectId: string; syncType: string }) => {
    return await githubApi.syncData(projectId, syncType);
  }
);

const githubSlice = createSlice({
  name: 'github',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
    clearOverview: (state) => {
      state.overview = null;
      state.lastUpdated = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchProjectOverview.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchProjectOverview.fulfilled, (state, action) => {
        state.loading = false;
        state.overview = action.payload;
        state.lastUpdated = Date.now();
      })
      .addCase(fetchProjectOverview.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to fetch overview';
      });
  },
});

export const { clearError, clearOverview } = githubSlice.actions;
export default githubSlice.reducer;
```

### Zustand Implementation

```typescript
// store/githubStore.ts
import { create } from 'zustand';
import { githubApi } from '../api/github';

interface GitHubStore {
  overview: GitHubProjectOverviewResponseDto | null;
  loading: boolean;
  error: string | null;
  lastUpdated: number | null;
  
  fetchOverview: (projectId: string, daysPeriod: number) => Promise<void>;
  syncData: (projectId: string, syncType?: string) => Promise<void>;
  clearError: () => void;
  clearOverview: () => void;
}

export const useGitHubStore = create<GitHubStore>((set, get) => ({
  overview: null,
  loading: false,
  error: null,
  lastUpdated: null,
  
  fetchOverview: async (projectId: string, daysPeriod: number) => {
    set({ loading: true, error: null });
    
    try {
      const overview = await githubApi.getProjectOverview(projectId, daysPeriod);
      set({ overview, loading: false, lastUpdated: Date.now() });
    } catch (error) {
      set({ 
        error: error instanceof Error ? error.message : 'Failed to fetch overview',
        loading: false 
      });
    }
  },
  
  syncData: async (projectId: string, syncType = 'all') => {
    try {
      await githubApi.syncData(projectId, syncType);
      // Optionally refresh overview after sync
      await get().fetchOverview(projectId, 30);
    } catch (error) {
      set({ 
        error: error instanceof Error ? error.message : 'Sync failed'
      });
    }
  },
  
  clearError: () => set({ error: null }),
  clearOverview: () => set({ overview: null, lastUpdated: null }),
}));
```

## 🎨 CSS Styling

```css
/* styles/github-dashboard.css */
.github-dashboard {
  padding: 20px;
  max-width: 1200px;
  margin: 0 auto;
}

.dashboard-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 30px;
  padding-bottom: 20px;
  border-bottom: 1px solid #e1e5e9;
}

.dashboard-actions {
  display: flex;
  gap: 10px;
}

.dashboard-actions button {
  padding: 8px 16px;
  border: 1px solid #d1d5da;
  border-radius: 6px;
  background: #f6f8fa;
  cursor: pointer;
  transition: all 0.2s;
}

.dashboard-actions button:hover {
  background: #e1e5e9;
}

.dashboard-actions button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.repository-info-card {
  background: white;
  border: 1px solid #d1d5da;
  border-radius: 8px;
  padding: 20px;
  margin-bottom: 20px;
}

.repository-header {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 15px;
}

.repository-header h3 {
  margin: 0;
  font-size: 24px;
  color: #24292e;
}

.owner {
  color: #586069;
  font-size: 16px;
}

.repository-stats {
  display: flex;
  gap: 20px;
  margin: 15px 0;
}

.stat {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.stat-value {
  font-size: 20px;
  font-weight: 600;
  color: #24292e;
}

.stat-label {
  font-size: 12px;
  color: #586069;
  text-transform: uppercase;
}

.statistics-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 20px;
  margin-bottom: 20px;
}

.stat-card {
  background: white;
  border: 1px solid #d1d5da;
  border-radius: 8px;
  padding: 20px;
}

.stat-card h4 {
  margin: 0 0 15px 0;
  color: #24292e;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(100px, 1fr));
  gap: 15px;
}

.contributions-table {
  background: white;
  border: 1px solid #d1d5da;
  border-radius: 8px;
  padding: 20px;
  margin-bottom: 20px;
}

.contributions-table h3 {
  margin: 0 0 20px 0;
  color: #24292e;
}

.table-container {
  overflow-x: auto;
}

table {
  width: 100%;
  border-collapse: collapse;
}

th, td {
  padding: 12px;
  text-align: left;
  border-bottom: 1px solid #e1e5e9;
}

th {
  background: #f6f8fa;
  font-weight: 600;
  cursor: pointer;
  user-select: none;
}

th:hover {
  background: #e1e5e9;
}

tr:hover {
  background: #f6f8fa;
}

.repository-link {
  background: white;
  border: 1px solid #d1d5da;
  border-radius: 8px;
  padding: 20px;
  margin-bottom: 20px;
}

.link-form {
  display: flex;
  gap: 10px;
  margin-bottom: 15px;
}

.link-form input {
  flex: 1;
  padding: 8px 12px;
  border: 1px solid #d1d5da;
  border-radius: 6px;
  font-size: 14px;
}

.link-form button {
  padding: 8px 16px;
  border: 1px solid #d1d5da;
  border-radius: 6px;
  background: #f6f8fa;
  cursor: pointer;
}

.validation-result {
  padding: 10px;
  border-radius: 6px;
  margin-bottom: 10px;
}

.validation-result.valid {
  background: #d4edda;
  border: 1px solid #c3e6cb;
  color: #155724;
}

.validation-result.invalid {
  background: #f8d7da;
  border: 1px solid #f5c6cb;
  color: #721c24;
}

.error-message {
  background: #f8d7da;
  border: 1px solid #f5c6cb;
  color: #721c24;
  padding: 10px;
  border-radius: 6px;
}

.loading-spinner {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 200px;
}

.no-data {
  text-align: center;
  color: #586069;
  padding: 40px;
}
```

This implementation guide provides everything needed to build a comprehensive GitHub integration frontend with proper error handling, loading states, and responsive design.
