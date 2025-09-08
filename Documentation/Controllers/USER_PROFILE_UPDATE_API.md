# User Profile Update API Documentation

## Overview

This document describes the API endpoints for updating user profiles and adding expertise in a single API call. Users can update their basic profile information and optionally include expertise details in the same request.

## Base URL

```
https://gainitwebapp-dvhfcxbkezgyfwf6.israelcentral-01.azurewebsites.net/api/users
```

## Authentication

All endpoints require JWT authentication with the `RequireAccessAsUser` policy.

## Common Profile Fields

All user types share these common profile fields:

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| `fullName` | string | ✅ | max 100 chars | User's full name |
| `biography` | string | ✅ | max 1000 chars | User's biography/description |
| `facebookPageURL` | string | ❌ | valid URL, max 200 chars | Facebook profile URL |
| `linkedInURL` | string | ❌ | valid URL, max 200 chars | LinkedIn profile URL |
| `gitHubURL` | string | ❌ | valid URL, max 200 chars | GitHub profile URL |
| `gitHubUsername` | string | ❌ | max 100 chars | GitHub username |
| `profilePictureURL` | string | ❌ | valid URL, max 200 chars | Profile picture URL |

---

## 1. Gainer Profile Update

### Endpoint
```
PUT /api/users/gainer/{id}/profile
```

### Gainer-Specific Fields

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| `currentRole` | string | ✅ | max 100 chars | Current job role/position |
| `yearsOfExperience` | integer | ✅ | range 0-50 | Years of professional experience |
| `educationStatus` | string | ✅ | - | Education level (e.g., "Bachelor's", "Master's") |
| `areasOfInterest` | string[] | ✅ | min 1 item, max 1000 chars total | Areas of interest/specialization |

### Optional Expertise Fields

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| `programmingLanguages` | string[] | ❌ | - | Programming languages known |
| `technologies` | string[] | ❌ | - | Technologies/frameworks familiar with |
| `tools` | string[] | ❌ | - | Development tools used |

### Complete Request Example

```json
{
  "fullName": "John Developer",
  "biography": "Passionate software developer with 3 years of experience in web development. Love working with modern technologies and solving complex problems.",
  "facebookPageURL": "https://facebook.com/johndeveloper",
  "linkedInURL": "https://linkedin.com/in/johndeveloper",
  "gitHubURL": "https://github.com/johndeveloper",
  "gitHubUsername": "johndeveloper",
  "profilePictureURL": "https://example.com/profile.jpg",
  "currentRole": "Full Stack Developer",
  "yearsOfExperience": 3,
  "educationStatus": "Bachelor's Degree in Computer Science",
  "areasOfInterest": ["Web Development", "Mobile Apps", "Cloud Computing"],
  "programmingLanguages": ["C#", "JavaScript", "TypeScript", "Python"],
  "technologies": ["ASP.NET Core", "React", "Node.js", "Docker"],
  "tools": ["Visual Studio", "VS Code", "Git", "Postman"]
}
```

---

## 2. Mentor Profile Update

### Endpoint
```
PUT /api/users/mentor/{id}/profile
```

### Mentor-Specific Fields

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| `yearsOfExperience` | integer | ✅ | range 1-50 | Years of mentoring experience |
| `areaOfExpertise` | string | ✅ | max 200 chars | Primary area of expertise |

### Optional Expertise Fields

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| `programmingLanguages` | string[] | ❌ | - | Programming languages to mentor |
| `technologies` | string[] | ❌ | - | Technologies to mentor |
| `tools` | string[] | ❌ | - | Tools to mentor |

### Complete Request Example

```json
{
  "fullName": "Sarah Mentor",
  "biography": "Senior software architect with 15 years of experience. Passionate about mentoring junior developers and sharing knowledge.",
  "facebookPageURL": "https://facebook.com/sarahmentor",
  "linkedInURL": "https://linkedin.com/in/sarahmentor",
  "gitHubURL": "https://github.com/sarahmentor",
  "gitHubUsername": "sarahmentor",
  "profilePictureURL": "https://example.com/sarah.jpg",
  "yearsOfExperience": 15,
  "areaOfExpertise": "Software Architecture and System Design",
  "programmingLanguages": ["C#", "Java", "Go", "Rust"],
  "technologies": ["Microservices", "Cloud Architecture", "DevOps", "Security"],
  "tools": ["Azure", "AWS", "Kubernetes", "Docker", "Jenkins"]
}
```

---

## 3. Nonprofit Organization Profile Update

### Endpoint
```
PUT /api/users/nonprofit/{id}/profile
```

### Nonprofit-Specific Fields

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| `websiteUrl` | string | ✅ | valid URL | Organization's website URL |

### Optional Expertise Fields

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| `fieldOfWork` | string | ❌ | max 200 chars | Primary field of work |
| `missionStatement` | string | ❌ | max 1000 chars | Organization's mission statement |

### Complete Request Example

```json
{
  "fullName": "Tech for Good Foundation",
  "biography": "Nonprofit organization dedicated to using technology to solve social problems and improve lives in underserved communities.",
  "facebookPageURL": "https://facebook.com/techforgood",
  "linkedInURL": "https://linkedin.com/company/techforgood",
  "gitHubURL": "https://github.com/techforgood",
  "gitHubUsername": "techforgood",
  "profilePictureURL": "https://example.com/logo.png",
  "websiteUrl": "https://techforgood.org",
  "fieldOfWork": "Education and Technology",
  "missionStatement": "To bridge the digital divide by providing technology education and resources to underserved communities, empowering individuals to create positive change through innovation."
}
```

---

## Response Format

### Success Response (200 OK)
Returns the updated user entity with all profile information.

### Error Responses

| Status Code | Description |
|-------------|-------------|
| 400 | Bad Request - Invalid data or validation errors |
| 401 | Unauthorized - Missing or invalid JWT token |
| 404 | Not Found - User with specified ID not found |
| 500 | Internal Server Error - Server-side error |

### Validation Error Example

```json
{
  "errors": {
    "fullName": ["Full Name is required"],
    "yearsOfExperience": ["Years of Experience must be between 0 and 50"],
    "facebookPageURL": ["Invalid Facebook URL"]
  }
}
```

---

## How It Works

1. **Profile Update**: The API first updates the basic profile information using the profile update service
2. **Expertise Addition**: If expertise fields are provided, the API automatically calls the appropriate expertise service
3. **Transaction**: Both operations are performed in sequence, ensuring data consistency
4. **Response**: Returns the updated user profile

## Best Practices

1. **Required Fields**: Always include required fields in your request
2. **Optional Fields**: Only include expertise fields if you want to add/update expertise
3. **URL Validation**: Ensure all URL fields are properly formatted
4. **Data Types**: Use correct data types (strings for text, integers for numbers, arrays for lists)
5. **Field Limits**: Respect the maximum character limits for each field

## Testing

### Test with Minimal Data
```json
{
  "fullName": "Test User",
  "biography": "Test biography",
  "currentRole": "Developer",
  "yearsOfExperience": 1,
  "educationStatus": "Bachelor's",
  "areasOfInterest": ["Testing"]
}
```

### Test with Full Data
Include all optional fields to test the complete functionality.

### Test Expertise Only
You can also add expertise separately using the dedicated expertise endpoints:
- `POST /api/users/gainer/{id}/expertise`
- `POST /api/users/mentor/{id}/expertise`
- `POST /api/users/nonprofit/{id}/expertise`

---

## Support

For questions or issues with the User Profile Update API, please contact the development team or refer to the API logs for detailed error information.
