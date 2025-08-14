# User Profile Management Logging Implementation

## Overview
Comprehensive logging has been implemented across all user profile management operations including Gainers, Mentors, and Nonprofit Organizations. This logging provides visibility into user profile operations, expertise management, and achievement tracking.

## Files with Logging

### 1. `Services/Users/Implementations/UserProfileService.cs`

#### **Gainer Profile Operations**
**Methods with Logging:**
- `GetGainerByIdAsync()`
- `UpdateGainerProfileAsync()`
- `GetGainerExpertiseAsync()`
- `AddExpertiseToGainerAsync()`
- `GetGainerAchievementsAsync()`
- `AddAchievementToGainerAsync()`
- `GetGainerProjectHistoryAsync()`

**Logging Examples:**
```csharp
// Profile retrieval
r_logger.LogInformation("Getting Gainer by ID: UserId={UserId}", i_userId);
r_logger.LogWarning("Gainer not found: UserId={UserId}", i_userId);
r_logger.LogInformation("Successfully retrieved Gainer: UserId={UserId}", i_userId);

// Profile updates
r_logger.LogInformation("Updating Gainer profile: UserId={UserId}", i_userId);
r_logger.LogInformation("Successfully updated Gainer profile: UserId={UserId}", i_userId);

// Expertise management
r_logger.LogInformation("Adding expertise to Gainer: UserId={UserId}", userId);
r_logger.LogDebug("Gainer does not have TechExpertise. Creating new TechExpertise for Gainer: UserId={UserId}", userId);
r_logger.LogInformation("Successfully added expertise to Gainer: UserId={UserId}", userId);

// Achievement management
r_logger.LogInformation("Adding achievement to Gainer: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", userId, achievementTemplateId);
r_logger.LogWarning("Achievement template not found: AchievementTemplateId={AchievementTemplateId}", achievementTemplateId);
r_logger.LogInformation("Successfully added achievement to Gainer: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", userId, achievementTemplateId);
```

#### **Mentor Profile Operations**
**Methods with Logging:**
- `GetMentorByIdAsync()`
- `UpdateMentorProfileAsync()`
- `GetMentorExpertiseAsync()`
- `AddExpertiseToMentorAsync()`
- `GetMentorAchievementsAsync()`
- `AddAchievementToMentorAsync()`
- `GetMentorProjectHistoryAsync()`

**Logging Examples:**
```csharp
// Profile retrieval
r_logger.LogInformation("Getting Mentor by ID: UserId={UserId}", i_userId);
r_logger.LogWarning("Mentor not found: UserId={UserId}", i_userId);
r_logger.LogInformation("Successfully retrieved Mentor: UserId={UserId}", i_userId);

// Profile updates
r_logger.LogInformation("Updating Mentor profile: UserId={UserId}", userId);
r_logger.LogDebug("Mentor does not have TechExpertise. Creating new TechExpertise for Mentor: UserId={UserId}", userId);
r_logger.LogInformation("Successfully updated Mentor profile: UserId={UserId}", userId);

// Expertise management
r_logger.LogInformation("Adding expertise to Mentor: UserId={UserId}", userId);
r_logger.LogInformation("Successfully added expertise to Mentor: UserId={UserId}", userId);
```

#### **Nonprofit Profile Operations**
**Methods with Logging:**
- `GetNonprofitByIdAsync()`
- `UpdateNonprofitProfileAsync()`
- `GetNonprofitExpertiseAsync()`
- `AddExpertiseToNonprofitAsync()`
- `GetNonprofitAchievementsAsync()`
- `AddAchievementToNonprofitAsync()`
- `GetNonprofitProjectHistoryAsync()`
- `GetNonprofitOwnedProjectsAsync()`

**Logging Examples:**
```csharp
// Profile retrieval
r_logger.LogInformation("Getting Nonprofit by ID: UserId={UserId}", i_userId);
r_logger.LogWarning("Nonprofit not found: UserId={UserId}", i_userId);
r_logger.LogInformation("Successfully retrieved Nonprofit: UserId={UserId}", i_userId);

// Profile updates
r_logger.LogInformation("Updating Nonprofit profile: UserId={UserId}", userId);
r_logger.LogDebug("NonprofitExpertise is null for Nonprofit: UserId={UserId}. Creating new NonprofitExpertise.", userId);
r_logger.LogDebug("Created new NonprofitExpertise for Nonprofit: UserId={UserId}", userId);
r_logger.LogInformation("Successfully updated Nonprofit profile: UserId={UserId}", userId);

// Expertise management
r_logger.LogInformation("Adding expertise to Nonprofit: UserId={UserId}, FieldOfWork={FieldOfWork}", userId, expertise.FieldOfWork);
r_logger.LogDebug("Nonprofit does not have NonprofitExpertise. Creating new NonprofitExpertise for Nonprofit: UserId={UserId}", userId);
r_logger.LogInformation("Successfully added expertise to Nonprofit: UserId={UserId}, FieldOfWork={FieldOfWork}", userId, expertise.FieldOfWork);
```

#### **General User Operations**
**Methods with Logging:**
- `GetUserProjectsAsync()`
- `GetUserAchievementsAsync()`

**Logging Examples:**
```csharp
// Project retrieval
r_logger.LogInformation("Getting user projects: UserId={UserId}", userId);
r_logger.LogInformation("Retrieved user projects: UserId={UserId}, Count={Count}", userId, projects.Count);

// Achievement retrieval
r_logger.LogInformation("Getting user achievements: UserId={UserId}", userId);
r_logger.LogInformation("Retrieved user achievements: UserId={UserId}, Count={Count}", userId, achievements.Count);
```

### 2. `Controllers/Users/UsersController .cs`

#### **Profile Retrieval Endpoints**
**Methods with Logging:**
- `GetGainerProfile()`
- `GetMentorProfile()`
- `GetNonprofitProfile()`

**Logging Examples:**
```csharp
// Profile retrieval start
r_logger.LogInformation("Getting Gainer profile: UserId={UserId}", id);

// Validation warnings
r_logger.LogWarning("Invalid Gainer ID provided: {UserId}", id);
r_logger.LogWarning("Gainer not found: {UserId}", id);

// Success logging
r_logger.LogInformation("Successfully retrieved Gainer profile: UserId={UserId}, ProjectsCount={ProjectsCount}, AchievementsCount={AchievementsCount}", 
    id, projects.Count(), achievements.Count);

// Error logging
r_logger.LogError(ex, "Error retrieving Gainer profile: UserId={UserId}", id);
```

## Log Levels Used

- **Information**: Profile operations, successful retrievals, updates, and operations
- **Warning**: Validation issues, not found scenarios, missing data
- **Error**: Exceptions and failures during operations
- **Debug**: Detailed technical information, expertise creation, field updates

## Key Log Fields

### **User Identification**
- `UserId`: Primary identifier for all operations
- `ExternalId`: External authentication identifier
- `Email`: User email address

### **Operation Context**
- `ProjectsCount`: Number of projects associated with user
- `AchievementsCount`: Number of achievements earned
- `Changes`: List of fields updated during profile updates
- `Duration`: Time taken for operations

### **Expertise Management**
- `FieldOfWork`: Nonprofit field of work
- `MissionStatement`: Nonprofit mission statement
- `ProgrammingLanguages`: Technical programming languages
- `Technologies`: Technical technologies
- `Tools`: Technical tools

### **Achievement Management**
- `AchievementTemplateId`: Template identifier for achievements
- `EarnedAtUtc`: When achievement was earned

## Azure App Insights Benefits

### 1. **User Activity Monitoring**
- Track profile update frequency
- Monitor expertise additions
- Track achievement progress
- Monitor project participation

### 2. **Performance Monitoring**
- Profile retrieval response times
- Update operation performance
- Expertise and achievement operation timing

### 3. **Business Intelligence**
- User engagement metrics
- Expertise distribution analysis
- Achievement completion rates
- Project participation patterns

### 4. **Error Diagnostics**
- Profile update failures
- Expertise addition issues
- Achievement assignment problems
- Data validation failures

## Example Log Output

### Successful Profile Update
```
[Information] Updating Gainer profile: UserId=12345-67890-abcde
[Information] Successfully updated Gainer profile: UserId=12345-67890-abcde
```

### Expertise Addition
```
[Information] Adding expertise to Gainer: UserId=12345-67890-abcde
[Debug] Gainer does not have TechExpertise. Creating new TechExpertise for Gainer: UserId=12345-67890-abcde
[Information] Successfully added expertise to Gainer: UserId=12345-67890-abcde
```

### Profile Retrieval with Projects
```
[Information] Getting Gainer profile: UserId=12345-67890-abcde
[Information] Successfully retrieved Gainer profile: UserId=12345-67890-abcde, ProjectsCount=5, AchievementsCount=3
```

## Monitoring Queries for Azure App Insights

### Profile Update Monitoring
```
traces
| where message contains "Successfully updated"
| summarize count() by customDimensions.UserId, bin(timestamp, 1h)
```

### Expertise Addition Monitoring
```
traces
| where message contains "Successfully added expertise"
| summarize count() by bin(timestamp, 1h)
```

### Achievement Progress Monitoring
```
traces
| where message contains "Successfully added achievement"
| summarize count() by customDimensions.UserId, bin(timestamp, 1h)
```

### Error Rate Monitoring
```
exceptions
| where message contains "Error retrieving" or "Error updating"
| summarize count() by bin(timestamp, 1h)
```

## Best Practices Implemented

1. **Structured Logging**: All logs use structured parameters for better querying
2. **User Context**: Every log includes UserId for easy tracking
3. **Operation Tracking**: Start and completion of all major operations
4. **Performance Metrics**: Timing information for performance monitoring
5. **Error Context**: Full context for debugging and troubleshooting
6. **Business Metrics**: Counts and statistics for business intelligence
7. **Consistent Format**: All logs follow the same format for easier analysis

This logging implementation provides comprehensive visibility into user profile management operations, enabling effective monitoring, debugging, and business intelligence when deployed to Azure Web App with Application Insights. 