# Vector Search Frontend Integration Guide

## Overview
This guide explains how to integrate the two vector search endpoints in your frontend application. These endpoints use AI to find the most relevant projects based on user input or user profiles.

## Backend Vector Search Endpoints

### 1. Text-Based Vector Search
**Endpoint:** `GET /api/projects/search/vector`

**Purpose:** Find projects based on user's text query using AI semantic search.

**Parameters:**
- `query` (string, required): User's search text
- `count` (integer, optional): Number of projects to return (default: 3)

**Example:**
```
GET /api/projects/search/vector?query=machine%20learning%20projects&count=5
```

### 2. Profile-Based Vector Search
**Endpoint:** `GET /api/projects/match/profile`

**Purpose:** Find projects that match the current user's profile (skills, interests, experience) automatically.

**Parameters:**
- `count` (integer, optional): Number of projects to return (default: 3)

**Authentication:** Requires user to be logged in (JWT token)

**Example:**
```
GET /api/projects/match/profile?count=3
```

## Important: Count Parameter Behavior

**CRITICAL CHANGE:** The `count` parameter now controls **exactly how many projects you get back**.

- **Before:** `count=5` might return 3 projects (after AI filtering)
- **Now:** `count=5` **always returns exactly 5 projects**

This makes the API predictable and reliable for frontend pagination and UI design.

## Response Format

### Text Search Response
```json
{
  "projects": [
    {
      "projectId": "string",
      "projectName": "string",
      "projectDescription": "string",
      "difficultyLevel": "string",
      "durationDays": 30,
      "goals": ["string"],
      "technologies": ["string"],
      "requiredRoles": ["string"],
      "programmingLanguages": ["string"],
      "projectSource": "string",
      "projectStatus": "string",
      "ragContext": {
        "searchableText": "string",
        "tags": ["string"],
        "skillLevels": ["string"],
        "projectType": "string",
        "domain": "string",
        "learningOutcomes": ["string"],
        "complexityFactors": ["string"]
      }
    }
  ],
  "explanation": "AI-generated explanation of why these projects match the query"
}
```

### Profile Search Response
```json
[
  {
    "projectId": "string",
    "projectName": "string",
    "projectDescription": "string",
    "difficultyLevel": "string",
    "durationDays": 30,
    "goals": ["string"],
    "technologies": ["string"],
    "requiredRoles": ["string"],
    "programmingLanguages": ["string"],
    "projectSource": "string",
    "projectStatus": "string",
    "ragContext": {
      "searchableText": "string",
      "tags": ["string"],
      "skillLevels": ["string"],
      "projectType": "string",
      "domain": "string",
      "learningOutcomes": ["string"],
      "complexityFactors": ["string"]
    }
  }
]
```

## Frontend Implementation

### 1. Text Search Service

```javascript
// services/projectSearchService.js
class ProjectSearchService {
    constructor(baseUrl, authService) {
        this.baseUrl = baseUrl;
        this.authService = authService;
    }

    /**
     * Search projects using text query
     * @param {string} query - User's search text
     * @param {number} count - Number of projects to return (default: 3)
     * @returns {Promise<Object>} Search results with projects and explanation
     */
    async searchProjectsByText(query, count = 3) {
        try {
            const encodedQuery = encodeURIComponent(query);
            const response = await fetch(
                `${this.baseUrl}/api/projects/search/vector?query=${encodedQuery}&count=${count}`,
                {
                    method: 'GET',
                    headers: {
                        'Content-Type': 'application/json',
                    }
                }
            );

            if (!response.ok) {
                throw new Error(`Search failed: ${response.status} ${response.statusText}`);
            }

            const data = await response.json();
            
            // Validate response structure
            if (!data.projects || !Array.isArray(data.projects)) {
                throw new Error('Invalid response format');
            }

            return {
                projects: data.projects,
                explanation: data.explanation || '',
                totalCount: data.projects.length,
                requestedCount: count
            };

        } catch (error) {
            console.error('Text search error:', error);
            throw error;
        }
    }

    /**
     * Get projects matched to user's profile
     * @param {number} count - Number of projects to return (default: 3)
     * @returns {Promise<Array>} Array of matched projects
     */
    async getProfileMatches(count = 3) {
        try {
            const token = this.authService.getToken();
            if (!token) {
                throw new Error('User must be logged in for profile matching');
            }

            const response = await fetch(
                `${this.baseUrl}/api/projects/match/profile?count=${count}`,
                {
                    method: 'GET',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${token}`
                    }
                }
            );

            if (!response.ok) {
                if (response.status === 401) {
                    throw new Error('Authentication required for profile matching');
                }
                throw new Error(`Profile matching failed: ${response.status} ${response.statusText}`);
            }

            const projects = await response.json();
            
            // Validate response structure
            if (!Array.isArray(projects)) {
                throw new Error('Invalid response format');
            }

            return {
                projects: projects,
                totalCount: projects.length,
                requestedCount: count
            };

        } catch (error) {
            console.error('Profile matching error:', error);
            throw error;
        }
    }
}

export default ProjectSearchService;
```

### 2. React Hook for Vector Search

```javascript
// hooks/useVectorSearch.js
import { useState, useCallback } from 'react';
import ProjectSearchService from '../services/projectSearchService';

export const useVectorSearch = (baseUrl, authService) => {
    const [searchService] = useState(() => new ProjectSearchService(baseUrl, authService));
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);

    const searchByText = useCallback(async (query, count = 3) => {
        if (!query || query.trim().length === 0) {
            setError('Search query cannot be empty');
            return null;
        }

        setLoading(true);
        setError(null);

        try {
            const results = await searchService.searchProjectsByText(query.trim(), count);
            return results;
        } catch (err) {
            setError(err.message);
            return null;
        } finally {
            setLoading(false);
        }
    }, [searchService]);

    const getProfileMatches = useCallback(async (count = 3) => {
        setLoading(true);
        setError(null);

        try {
            const results = await searchService.getProfileMatches(count);
            return results;
        } catch (err) {
            setError(err.message);
            return null;
        } finally {
            setLoading(false);
        }
    }, [searchService]);

    return {
        searchByText,
        getProfileMatches,
        loading,
        error,
        clearError: () => setError(null)
    };
};
```

### 3. Text Search Component

```jsx
// components/VectorSearchComponent.jsx
import React, { useState } from 'react';
import { Search, Loader, AlertCircle, Lightbulb } from 'lucide-react';
import { useVectorSearch } from '../hooks/useVectorSearch';

const VectorSearchComponent = ({ baseUrl, authService, onResults }) => {
    const [query, setQuery] = useState('');
    const [count, setCount] = useState(3);
    const { searchByText, loading, error, clearError } = useVectorSearch(baseUrl, authService);

    const handleSearch = async (e) => {
        e.preventDefault();
        if (!query.trim()) return;

        const results = await searchByText(query, count);
        if (results && onResults) {
            onResults(results);
        }
    };

    return (
        <div className="w-full max-w-2xl mx-auto">
            <form onSubmit={handleSearch} className="space-y-4">
                {/* Search Input */}
                <div className="relative">
                    <input
                        type="text"
                        value={query}
                        onChange={(e) => {
                            setQuery(e.target.value);
                            clearError();
                        }}
                        placeholder="Describe what kind of project you're looking for..."
                        className="w-full px-4 py-3 pl-12 pr-4 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                        disabled={loading}
                    />
                    <Search className="absolute left-4 top-1/2 transform -translate-y-1/2 text-gray-400 w-5 h-5" />
                </div>

                {/* Count Selector */}
                <div className="flex items-center space-x-4">
                    <label className="text-sm font-medium text-gray-700">
                        Number of results:
                    </label>
                    <select
                        value={count}
                        onChange={(e) => setCount(parseInt(e.target.value))}
                        className="px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500"
                        disabled={loading}
                    >
                        <option value={3}>3 projects</option>
                        <option value={5}>5 projects</option>
                        <option value={10}>10 projects</option>
                    </select>
                </div>

                {/* Search Button */}
                <button
                    type="submit"
                    disabled={loading || !query.trim()}
                    className="w-full bg-blue-600 text-white py-3 px-4 rounded-lg hover:bg-blue-700 focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center space-x-2"
                >
                    {loading ? (
                        <>
                            <Loader className="w-5 h-5 animate-spin" />
                            <span>Searching...</span>
                        </>
                    ) : (
                        <>
                            <Search className="w-5 h-5" />
                            <span>Search Projects</span>
                        </>
                    )}
                </button>
            </form>

            {/* Error Display */}
            {error && (
                <div className="mt-4 p-4 bg-red-50 border border-red-200 rounded-lg flex items-start space-x-3">
                    <AlertCircle className="w-5 h-5 text-red-500 mt-0.5" />
                    <div>
                        <h3 className="text-sm font-medium text-red-800">Search Error</h3>
                        <p className="text-sm text-red-700 mt-1">{error}</p>
                    </div>
                </div>
            )}

            {/* Search Tips */}
            <div className="mt-6 p-4 bg-blue-50 border border-blue-200 rounded-lg">
                <div className="flex items-start space-x-3">
                    <Lightbulb className="w-5 h-5 text-blue-500 mt-0.5" />
                    <div>
                        <h3 className="text-sm font-medium text-blue-800">Search Tips</h3>
                        <ul className="text-sm text-blue-700 mt-1 space-y-1">
                            <li>• Be specific: "React web app with authentication"</li>
                            <li>• Mention technologies: "Python machine learning project"</li>
                            <li>• Describe your goals: "Learn full-stack development"</li>
                            <li>• Include difficulty: "Beginner-friendly React project"</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default VectorSearchComponent;
```

### 4. Profile Match Component

```jsx
// components/ProfileMatchComponent.jsx
import React, { useState, useEffect } from 'react';
import { User, Loader, AlertCircle, RefreshCw } from 'lucide-react';
import { useVectorSearch } from '../hooks/useVectorSearch';

const ProfileMatchComponent = ({ baseUrl, authService, onResults }) => {
    const [count, setCount] = useState(3);
    const { getProfileMatches, loading, error, clearError } = useVectorSearch(baseUrl, authService);

    const handleGetMatches = async () => {
        const results = await getProfileMatches(count);
        if (results && onResults) {
            onResults(results);
        }
    };

    // Auto-load matches on component mount
    useEffect(() => {
        handleGetMatches();
    }, []);

    return (
        <div className="w-full max-w-2xl mx-auto">
            <div className="text-center mb-6">
                <User className="w-12 h-12 text-blue-600 mx-auto mb-3" />
                <h2 className="text-2xl font-bold text-gray-900">Projects for You</h2>
                <p className="text-gray-600 mt-2">
                    AI-matched projects based on your profile, skills, and interests
                </p>
            </div>

            {/* Count Selector */}
            <div className="flex items-center justify-center space-x-4 mb-6">
                <label className="text-sm font-medium text-gray-700">
                    Show me:
                </label>
                <select
                    value={count}
                    onChange={(e) => setCount(parseInt(e.target.value))}
                    className="px-3 py-2 border border-gray-300 rounded-md focus:ring-2 focus:ring-blue-500"
                    disabled={loading}
                >
                    <option value={3}>3 projects</option>
                    <option value={5}>5 projects</option>
                    <option value={10}>10 projects</option>
                </select>
            </div>

            {/* Refresh Button */}
            <button
                onClick={handleGetMatches}
                disabled={loading}
                className="w-full bg-blue-600 text-white py-3 px-4 rounded-lg hover:bg-blue-700 focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center space-x-2"
            >
                {loading ? (
                    <>
                        <Loader className="w-5 h-5 animate-spin" />
                        <span>Finding matches...</span>
                    </>
                ) : (
                    <>
                        <RefreshCw className="w-5 h-5" />
                        <span>Refresh Matches</span>
                    </>
                )}
            </button>

            {/* Error Display */}
            {error && (
                <div className="mt-4 p-4 bg-red-50 border border-red-200 rounded-lg flex items-start space-x-3">
                    <AlertCircle className="w-5 h-5 text-red-500 mt-0.5" />
                    <div>
                        <h3 className="text-sm font-medium text-red-800">Error</h3>
                        <p className="text-sm text-red-700 mt-1">{error}</p>
                    </div>
                </div>
            )}
        </div>
    );
};

export default ProfileMatchComponent;
```

### 5. Results Display Component

```jsx
// components/ProjectResultsComponent.jsx
import React from 'react';
import { Calendar, Users, Code, Target, Clock } from 'lucide-react';

const ProjectResultsComponent = ({ results, explanation, showExplanation = true }) => {
    if (!results || results.length === 0) {
        return (
            <div className="text-center py-8">
                <p className="text-gray-500">No projects found.</p>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            {/* AI Explanation (for text search) */}
            {explanation && showExplanation && (
                <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                    <h3 className="text-sm font-medium text-blue-800 mb-2">Why these projects match:</h3>
                    <p className="text-sm text-blue-700">{explanation}</p>
                </div>
            )}

            {/* Results Count */}
            <div className="text-sm text-gray-600">
                Found {results.length} project{results.length !== 1 ? 's' : ''}
            </div>

            {/* Projects Grid */}
            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                {results.map((project) => (
                    <div key={project.projectId} className="bg-white border border-gray-200 rounded-lg p-6 hover:shadow-md transition-shadow">
                        {/* Project Header */}
                        <div className="mb-4">
                            <h3 className="text-lg font-semibold text-gray-900 mb-2">
                                {project.projectName}
                            </h3>
                            <p className="text-sm text-gray-600 line-clamp-3">
                                {project.projectDescription}
                            </p>
                        </div>

                        {/* Project Details */}
                        <div className="space-y-3">
                            {/* Duration */}
                            <div className="flex items-center text-sm text-gray-600">
                                <Clock className="w-4 h-4 mr-2" />
                                {project.durationDays} days
                            </div>

                            {/* Difficulty */}
                            <div className="flex items-center text-sm text-gray-600">
                                <Target className="w-4 h-4 mr-2" />
                                {project.difficultyLevel}
                            </div>

                            {/* Technologies */}
                            {project.technologies && project.technologies.length > 0 && (
                                <div className="flex items-start text-sm text-gray-600">
                                    <Code className="w-4 h-4 mr-2 mt-0.5" />
                                    <div className="flex flex-wrap gap-1">
                                        {project.technologies.slice(0, 3).map((tech, index) => (
                                            <span key={index} className="bg-gray-100 px-2 py-1 rounded text-xs">
                                                {tech}
                                            </span>
                                        ))}
                                        {project.technologies.length > 3 && (
                                            <span className="text-xs text-gray-500">
                                                +{project.technologies.length - 3} more
                                            </span>
                                        )}
                                    </div>
                                </div>
                            )}

                            {/* Required Roles */}
                            {project.requiredRoles && project.requiredRoles.length > 0 && (
                                <div className="flex items-start text-sm text-gray-600">
                                    <Users className="w-4 h-4 mr-2 mt-0.5" />
                                    <div className="flex flex-wrap gap-1">
                                        {project.requiredRoles.slice(0, 2).map((role, index) => (
                                            <span key={index} className="bg-blue-100 text-blue-800 px-2 py-1 rounded text-xs">
                                                {role}
                                            </span>
                                        ))}
                                        {project.requiredRoles.length > 2 && (
                                            <span className="text-xs text-gray-500">
                                                +{project.requiredRoles.length - 2} more
                                            </span>
                                        )}
                                    </div>
                                </div>
                            )}
                        </div>

                        {/* Action Button */}
                        <div className="mt-4 pt-4 border-t border-gray-100">
                            <button
                                onClick={() => window.location.href = `/projects/${project.projectId}`}
                                className="w-full bg-blue-600 text-white py-2 px-4 rounded-md hover:bg-blue-700 focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 text-sm font-medium"
                            >
                                View Project
                            </button>
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
};

export default ProjectResultsComponent;
```

### 6. Complete Integration Example

```jsx
// pages/SearchPage.jsx
import React, { useState } from 'react';
import VectorSearchComponent from '../components/VectorSearchComponent';
import ProfileMatchComponent from '../components/ProfileMatchComponent';
import ProjectResultsComponent from '../components/ProjectResultsComponent';

const SearchPage = () => {
    const [searchResults, setSearchResults] = useState(null);
    const [profileResults, setProfileResults] = useState(null);
    const [activeTab, setActiveTab] = useState('search');

    const handleSearchResults = (results) => {
        setSearchResults(results);
    };

    const handleProfileResults = (results) => {
        setProfileResults(results);
    };

    return (
        <div className="min-h-screen bg-gray-50 py-8">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                {/* Page Header */}
                <div className="text-center mb-8">
                    <h1 className="text-3xl font-bold text-gray-900 mb-4">
                        Find Your Perfect Project
                    </h1>
                    <p className="text-lg text-gray-600">
                        Use AI-powered search to discover projects that match your interests and skills
                    </p>
                </div>

                {/* Tab Navigation */}
                <div className="flex justify-center mb-8">
                    <div className="bg-white rounded-lg p-1 shadow-sm">
                        <button
                            onClick={() => setActiveTab('search')}
                            className={`px-6 py-2 rounded-md text-sm font-medium transition-colors ${
                                activeTab === 'search'
                                    ? 'bg-blue-600 text-white'
                                    : 'text-gray-600 hover:text-gray-900'
                            }`}
                        >
                            Search Projects
                        </button>
                        <button
                            onClick={() => setActiveTab('profile')}
                            className={`px-6 py-2 rounded-md text-sm font-medium transition-colors ${
                                activeTab === 'profile'
                                    ? 'bg-blue-600 text-white'
                                    : 'text-gray-600 hover:text-gray-900'
                            }`}
                        >
                            For You
                        </button>
                    </div>
                </div>

                {/* Search Components */}
                <div className="mb-8">
                    {activeTab === 'search' ? (
                        <VectorSearchComponent
                            baseUrl={process.env.REACT_APP_API_URL}
                            authService={authService}
                            onResults={handleSearchResults}
                        />
                    ) : (
                        <ProfileMatchComponent
                            baseUrl={process.env.REACT_APP_API_URL}
                            authService={authService}
                            onResults={handleProfileResults}
                        />
                    )}
                </div>

                {/* Results */}
                {activeTab === 'search' && searchResults && (
                    <ProjectResultsComponent
                        results={searchResults.projects}
                        explanation={searchResults.explanation}
                        showExplanation={true}
                    />
                )}

                {activeTab === 'profile' && profileResults && (
                    <ProjectResultsComponent
                        results={profileResults.projects}
                        showExplanation={false}
                    />
                )}
            </div>
        </div>
    );
};

export default SearchPage;
```

## Key Implementation Notes

### 1. Count Parameter Reliability
- The `count` parameter now **always** returns exactly that many projects
- No more guessing or handling variable result counts
- Perfect for pagination and UI layout planning

### 2. Error Handling
- Always handle authentication errors for profile matching
- Provide user-friendly error messages
- Implement retry mechanisms for network failures

### 3. Loading States
- Vector search can take 2-3 seconds due to AI processing
- Show appropriate loading indicators
- Disable form inputs during search

### 4. User Experience
- Show AI explanations for text search results
- Provide search tips and examples
- Allow users to adjust result count
- Implement auto-refresh for profile matches

### 5. Performance
- Implement debouncing for search input
- Cache results when appropriate
- Use pagination for large result sets

This implementation provides a complete, production-ready vector search integration that leverages the improved count parameter behavior for a reliable user experience.
