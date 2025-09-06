# User Dashboard Endpoint Documentation

## Overview

The User Dashboard endpoint provides comprehensive analytics and metrics for a user's activity across the GainIt platform, including task completion, GitHub contributions, forum engagement, and skill development.

## Endpoint Details

- **URL:** `GET /api/users/{userId}/dashboard`
- **Authorization:** Requires `RequireAccessAsUser` policy
- **Content-Type:** `application/json`

## Request Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `userId` | `Guid` | Yes | The unique identifier of the user |

## Response Structure

The endpoint returns a comprehensive dashboard object with the following structure:

```json
{
  "commitsTimeline": [...],
  "collaborationIndex": {...},
  "completionTracker": {...},
  "refactoringRate": {...},
  "skillDistribution": {...},
  "collaborationActivity": [...],
  "achievements": [...],
  "communityCollaboration": {...}
}
```

## Detailed Response Fields

### 1. Commits Timeline (`commitsTimeline`)

**Type:** `Array<Object>`

Shows weekly commit activity aggregated across all user's projects.

```json
[
  {
    "week": "2024-W01",
    "commits": 15
  },
  {
    "week": "2024-W02", 
    "commits": 23
  }
]
```

**Fields:**
- `week` (string): Week identifier in format "YYYY-W##"
- `commits` (number): Total commits for that week

### 2. Collaboration Index (`collaborationIndex`)

**Type:** `Object`

Measures the user's collaborative coding activity.

```json
{
  "pullRequests": 12,
  "pushes": 45,
  "collaborationRatioPercentage": 21
}
```

**Fields:**
- `pullRequests` (number): Total pull requests created
- `pushes` (number): Total commits pushed
- `collaborationRatioPercentage` (number): Percentage of collaborative work (PRs vs total events)

### 3. Completion Tracker (`completionTracker`)

**Type:** `Object`

Tracks task completion performance on the platform.

```json
{
  "tasksStarted": 25,
  "tasksCompleted": 18,
  "completionRatePercentage": 72
}
```

**Fields:**
- `tasksStarted` (number): Total tasks assigned to the user
- `tasksCompleted` (number): Tasks marked as "Done"
- `completionRatePercentage` (number): Completion rate as percentage (0-100)

### 4. Refactoring Rate (`refactoringRate`)

**Type:** `Object`

Analyzes code quality through refactoring metrics.

```json
{
  "linesAdded": 1250,
  "linesDeleted": 320,
  "refactoringRatioPercentage": 20
}
```

**Fields:**
- `linesAdded` (number): Total lines of code added
- `linesDeleted` (number): Total lines of code deleted
- `refactoringRatioPercentage` (number): Percentage of code deletion (refactoring indicator)

### 5. Skill Distribution (`skillDistribution`)

**Type:** `Object`

Categorizes user's technical skills into four main areas.

```json
{
  "frontend": {
    "count": 8,
    "percentage": 40
  },
  "backend": {
    "count": 6,
    "percentage": 30
  },
  "database": {
    "count": 3,
    "percentage": 15
  },
  "devops": {
    "count": 3,
    "percentage": 15
  }
}
```

**Categories:**
- **Frontend:** JavaScript, TypeScript, React, Vue, Angular, HTML, CSS, Sass, Bootstrap, Tailwind
- **Backend:** Python, Java, C#, Node.js, Express, Django, Spring, ASP.NET, PHP, Ruby
- **Database:** SQL, PostgreSQL, MySQL, MongoDB, Redis, SQLite, Oracle, SQL Server
- **DevOps:** Docker, Kubernetes, AWS, Azure, Jenkins, Git, Terraform, Ansible, Nginx, Apache

**Fields:**
- `count` (number): Number of skills in this category
- `percentage` (number): Percentage of total skills (0-100)

### 6. Collaboration Activity (`collaborationActivity`)

**Type:** `Array<Object>`

Weekly breakdown of collaborative activities (PRs and issues).

```json
[
  {
    "week": "2024-W01",
    "pullRequests": 3,
    "issues": 2,
    "reviews": 0
  },
  {
    "week": "2024-W02",
    "pullRequests": 1,
    "issues": 4,
    "reviews": 0
  }
]
```

**Fields:**
- `week` (string): Week identifier
- `pullRequests` (number): Pull requests created this week
- `issues` (number): Issues created this week
- `reviews` (number): Reviews performed this week (currently always 0)

### 7. Achievements (`achievements`)

**Type:** `Array<Object>`

Gamification elements showing user progress toward platform goals.

```json
[
  {
    "title": "Task Master",
    "description": "Completed 50+ tasks",
    "earned": false,
    "progressPercentage": 36
  },
  {
    "title": "Code Reviewer", 
    "description": "Reviewed 25+ PRs",
    "earned": true,
    "progressPercentage": 100
  }
]
```

**Fields:**
- `title` (string): Achievement name
- `description` (string): Achievement description
- `earned` (boolean): Whether the achievement has been earned
- `progressPercentage` (number): Progress toward earning (0-100)

**Current Achievements:**
- **Task Master:** Complete 50+ tasks
- **Code Reviewer:** Review 25+ pull requests

### 8. Community Collaboration (`communityCollaboration`)

**Type:** `Object`

Measures engagement in the platform's community features.

```json
{
  "posts": 8,
  "replies": 23,
  "likesReceived": 45,
  "uniquePeersInteracted": 12
}
```

**Fields:**
- `posts` (number): Forum posts created
- `replies` (number): Forum replies posted
- `likesReceived` (number): Total likes received on posts and replies
- `uniquePeersInteracted` (number): Unique users interacted with (replied to posts, liked content, etc.)

## Data Sources

The dashboard aggregates data from multiple sources:

1. **Project Tasks** - Task assignments and completions
2. **Forum Posts/Replies** - Community engagement metrics
3. **Forum Likes** - Social interaction data
4. **GitHub Contributions** - Code contribution metrics
5. **GitHub Analytics** - Repository activity data
6. **Project Technologies** - Skill and technology usage

## Performance Considerations

- **Caching:** Dashboard data is not currently cached, so each request queries the database
- **Database Queries:** Multiple complex queries are executed for each dashboard request
- **Data Aggregation:** Weekly data is aggregated from multiple repositories
- **Real-time:** Data reflects current state of the database

## Error Responses

### 404 Not Found
```json
{
  "message": "User not found"
}
```

### 401 Unauthorized
```json
{
  "message": "Unauthorized access"
}
```

### 500 Internal Server Error
```json
{
  "message": "An error occurred while retrieving the dashboard data."
}
```

## Example Complete Response

```json
{
  "commitsTimeline": [
    {
      "week": "2024-W01",
      "commits": 15
    },
    {
      "week": "2024-W02",
      "commits": 23
    }
  ],
  "collaborationIndex": {
    "pullRequests": 12,
    "pushes": 45,
    "collaborationRatioPercentage": 21
  },
  "completionTracker": {
    "tasksStarted": 25,
    "tasksCompleted": 18,
    "completionRatePercentage": 72
  },
  "refactoringRate": {
    "linesAdded": 1250,
    "linesDeleted": 320,
    "refactoringRatioPercentage": 20
  },
  "skillDistribution": {
    "frontend": {
      "count": 8,
      "percentage": 40
    },
    "backend": {
      "count": 6,
      "percentage": 30
    },
    "database": {
      "count": 3,
      "percentage": 15
    },
    "devops": {
      "count": 3,
      "percentage": 15
    }
  },
  "collaborationActivity": [
    {
      "week": "2024-W01",
      "pullRequests": 3,
      "issues": 2,
      "reviews": 0
    }
  ],
  "achievements": [
    {
      "title": "Task Master",
      "description": "Completed 50+ tasks",
      "earned": false,
      "progressPercentage": 36
    },
    {
      "title": "Code Reviewer",
      "description": "Reviewed 25+ PRs",
      "earned": true,
      "progressPercentage": 100
    }
  ],
  "communityCollaboration": {
    "posts": 8,
    "replies": 23,
    "likesReceived": 45,
    "uniquePeersInteracted": 12
  }
}
```

## Usage Notes

1. **Authorization:** Users can only access their own dashboard data
2. **Data Freshness:** All metrics reflect real-time database state
3. **Empty States:** Zero values are returned as 0, not null
4. **Percentages:** All percentages are calculated and rounded to integers
5. **Time Periods:** Weekly data uses ISO week format (YYYY-W##)
6. **Skill Detection:** Skills are detected through project technology and language fields
7. **GitHub Integration:** Requires projects to have valid repository links for GitHub metrics

## Related Endpoints

- `GET /api/users/me` - Get current user profile
- `GET /api/users/me/summary` - Get AI-generated user summary
- `GET /api/users/{userId}/profile` - Get user profile by ID
