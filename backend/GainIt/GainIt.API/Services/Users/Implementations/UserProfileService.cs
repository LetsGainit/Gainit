using GainIt.API.Data;
using GainIt.API.DTOs.Requests.Users;
using GainIt.API.DTOs.ViewModels.Users;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;
using GainIt.API.Models.Users.Expertise;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users.Nonprofits;
using GainIt.API.Services.Users.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GainIt.API.Services.Users.Implementations
{
    public class UserProfileService : IUserProfileService
    {
        private readonly GainItDbContext r_DbContext;
        private readonly ILogger<UserProfileService> r_logger;

        public UserProfileService(GainItDbContext i_dbContext, ILogger<UserProfileService> i_logger)
        {
            r_DbContext = i_dbContext;
            r_logger = i_logger;
        }
        
        public async Task<UserProfileDto> GetOrCreateFromExternalAsync(ExternalUserDto i_externalUserDto)
        {
            var startTime = DateTimeOffset.UtcNow;
            r_logger.LogInformation("Starting external user provisioning: ExternalId={ExternalId}, Email={Email}, FullName={FullName}", 
                i_externalUserDto.ExternalId, i_externalUserDto.Email, i_externalUserDto.FullName);

            bool isNewUser = false;

            if (i_externalUserDto is null) 
            {
                r_logger.LogError("ExternalUserDto is null");
                throw new ArgumentNullException(nameof(i_externalUserDto));
            }
            
            if (string.IsNullOrWhiteSpace(i_externalUserDto.ExternalId))
            {
                r_logger.LogError("ExternalId (OID) is required but was empty or null");
                throw new ArgumentException("ExternalId (OID) is required.", nameof(i_externalUserDto.ExternalId));
            }

            var email = i_externalUserDto.Email?.Trim();
            var fullName = i_externalUserDto.FullName?.Trim();

            r_logger.LogDebug("Processing user data - Email: {Email}, FullName: {FullName}, Country: {Country}", 
                email, fullName, i_externalUserDto.Country);

            // Try find by ExternalId (OID)
            r_logger.LogDebug("Searching for existing user by ExternalId: {ExternalId}", i_externalUserDto.ExternalId);
            var dbSearchStartTime = DateTimeOffset.UtcNow;
            var user = await r_DbContext.Users.SingleOrDefaultAsync(u => u.ExternalId == i_externalUserDto.ExternalId);
            var dbSearchTime = DateTimeOffset.UtcNow.Subtract(dbSearchStartTime).TotalMilliseconds;
            r_logger.LogDebug("Database search completed: ExternalId={ExternalId}, UserFound={UserFound}, SearchTime={SearchTime}ms", 
                i_externalUserDto.ExternalId, user != null, dbSearchTime);

            if (user is null)
            {
                r_logger.LogInformation("User not found, creating new user: ExternalId={ExternalId}", i_externalUserDto.ExternalId);
                
                // For first-time provisioning your User requires EmailAddress:
                if (string.IsNullOrWhiteSpace(email))
                {
                    r_logger.LogError("Email is required for new user provisioning but was not provided: ExternalId={ExternalId}", i_externalUserDto.ExternalId);
                    throw new InvalidOperationException("Email is required for new user provisioning.");
                }

                var newUserId = Guid.NewGuid();
                r_logger.LogDebug("Creating new user with ID: {UserId}, ExternalId={ExternalId}, Email={Email}", 
                    newUserId, i_externalUserDto.ExternalId, email);

                user = new User
                {
                    UserId = newUserId,
                    ExternalId = i_externalUserDto.ExternalId,
                    EmailAddress = email!,
                    FullName = fullName ?? "Unknown",
                    Country = string.IsNullOrWhiteSpace(i_externalUserDto.Country) ? null : i_externalUserDto.Country,
                    GitHubUsername = i_externalUserDto.GitHubUsername,  // Add GitHub username
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = DateTimeOffset.UtcNow
                };

                isNewUser = true;

                r_DbContext.Users.Add(user);
                var dbCreateStartTime = DateTimeOffset.UtcNow;
                await r_DbContext.SaveChangesAsync();
                var dbCreateTime = DateTimeOffset.UtcNow.Subtract(dbCreateStartTime).TotalMilliseconds;
                
                r_logger.LogInformation("Successfully created new user: UserId={UserId}, ExternalId={ExternalId}, Email={Email}, CreatedAt={CreatedAt}, DbCreateTime={DbCreateTime}ms", 
                    user.UserId, user.ExternalId, user.EmailAddress, user.CreatedAt, dbCreateTime);
            }
            else
            {
                r_logger.LogInformation("Found existing user, updating profile: UserId={UserId}, ExternalId={ExternalId}, CurrentEmail={CurrentEmail}", 
                    user.UserId, user.ExternalId, user.EmailAddress);
                
                var changes = new List<string>();
                
                // Update basic fields if provided/changed
                if (!string.IsNullOrWhiteSpace(email) &&
                    !string.Equals(user.EmailAddress, email, StringComparison.OrdinalIgnoreCase))
                {
                    r_logger.LogDebug("Updating email from {OldEmail} to {NewEmail}", user.EmailAddress, email);
                    user.EmailAddress = email!;
                    changes.Add("Email");
                }

                if (!string.IsNullOrWhiteSpace(fullName) &&
                    !string.Equals(user.FullName, fullName, StringComparison.Ordinal))
                {
                    r_logger.LogDebug("Updating full name from {OldName} to {NewName}", user.FullName, fullName);
                    user.FullName = fullName;
                    changes.Add("FullName");
                }

                if (!string.IsNullOrWhiteSpace(i_externalUserDto.Country))
                {
                    r_logger.LogDebug("Updating country from {OldCountry} to {NewCountry}", user.Country, i_externalUserDto.Country);
                    user.Country = i_externalUserDto.Country;
                    changes.Add("Country");
                }

                user.LastLoginAt = DateTimeOffset.UtcNow;
                changes.Add("LastLoginAt");
                
                if (changes.Any())
                {
                    var dbUpdateStartTime = DateTimeOffset.UtcNow;
                    await r_DbContext.SaveChangesAsync();
                    var dbUpdateTime = DateTimeOffset.UtcNow.Subtract(dbUpdateStartTime).TotalMilliseconds;
                    
                    r_logger.LogInformation("Successfully updated existing user: UserId={UserId}, Changes={Changes}, LastLoginAt={LastLoginAt}, DbUpdateTime={DbUpdateTime}ms", 
                        user.UserId, string.Join(", ", changes), user.LastLoginAt, dbUpdateTime);
                }
                else
                {
                    r_logger.LogDebug("No changes detected for existing user: UserId={UserId}, ExternalId={ExternalId}", 
                        user.UserId, user.ExternalId);
                }
            }

            // Return minimal profile DTO
            var profileDto = new UserProfileDto
            {
                UserId = user.UserId,
                ExternalId = user.ExternalId,
                EmailAddress = user.EmailAddress,
                FullName = user.FullName,
                Country = user.Country,
                GitHubUsername = user.GitHubUsername,  // Add GitHub username
                IsNewUser = isNewUser
            };

            var totalTime = DateTimeOffset.UtcNow.Subtract(startTime).TotalMilliseconds;
            r_logger.LogDebug("Returning user profile DTO: UserId={UserId}, ExternalId={ExternalId}, Email={Email}, FullName={FullName}, TotalProcessingTime={TotalTime}ms", 
                profileDto.UserId, profileDto.ExternalId, profileDto.EmailAddress, profileDto.FullName, totalTime);

            return profileDto;
        }

        public async Task<Gainer> GetGainerByIdAsync(Guid i_userId)
        {
            r_logger.LogInformation("Getting Gainer by ID: UserId={UserId}", i_userId);

            try
            {
                var gainer = await r_DbContext.Gainers
                    .Include(g => g.TechExpertise)
                    .Include(g => g.Achievements)
                    .FirstOrDefaultAsync(g => g.UserId == i_userId);

                if (gainer == null)
                {
                    r_logger.LogWarning("Gainer not found: UserId={UserId}", i_userId);
                    throw new KeyNotFoundException($"Gainer with ID {i_userId} not found");
                }

                r_logger.LogInformation("Successfully retrieved Gainer: UserId={UserId}", i_userId);
                return gainer;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Gainer: UserId={UserId}", i_userId);
                throw;
            }
        }

        // NEW: Get all projects a user is participating in (via ProjectMembers)
        public async Task<List<UserProject>> GetUserProjectsAsync(Guid userId)
        {
            r_logger.LogInformation("Getting user projects: UserId={UserId}", userId);

            try
            {
                var projects = await r_DbContext.Projects
                    .Where(p => p.ProjectMembers.Any(pm => pm.UserId == userId && pm.LeftAtUtc == null))
                    .ToListAsync();

                r_logger.LogInformation("Retrieved user projects: UserId={UserId}, Count={Count}", userId, projects.Count);
                return projects;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving user projects: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task<Mentor> GetMentorByIdAsync(Guid i_userId)
        {
            r_logger.LogInformation("Getting Mentor by ID: UserId={UserId}", i_userId);

            try
            {
                // Load basic mentor data first
                var mentor = await r_DbContext.Mentors
                    .FirstOrDefaultAsync(m => m.UserId == i_userId);

                if (mentor == null)
                {
                    r_logger.LogWarning("Mentor not found: UserId={UserId}", i_userId);
                    throw new KeyNotFoundException($"Mentor with ID {i_userId} not found");
                }

                // Load collections separately to avoid the warning
                await r_DbContext.Entry(mentor)
                    .Reference(m => m.TechExpertise)
                    .LoadAsync();

                await r_DbContext.Entry(mentor)
                    .Collection(m => m.MentoredProjects)
                    .LoadAsync();

                await r_DbContext.Entry(mentor)
                    .Collection(m => m.Achievements)
                    .LoadAsync();

                r_logger.LogInformation("Successfully retrieved Mentor: UserId={UserId}", i_userId);
                return mentor;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Mentor: UserId={UserId}", i_userId);
                throw;
            }
        }

        public async Task<NonprofitOrganization> GetNonprofitByIdAsync(Guid i_userId)
        {
            r_logger.LogInformation("Getting Nonprofit by ID: UserId={UserId}", i_userId);

            try
            {
                var nonprofit = await r_DbContext.Nonprofits
                    .Include(n => n.NonprofitExpertise)
                    .Include(n => n.OwnedProjects)
                    .FirstOrDefaultAsync(n => n.UserId == i_userId);

                if (nonprofit == null)
                {
                    r_logger.LogWarning("Nonprofit not found: UserId={UserId}", i_userId);
                    throw new KeyNotFoundException($"Noneprofit with ID {i_userId} not found");
                }

                r_logger.LogInformation("Successfully retrieved Nonprofit: UserId={UserId}", i_userId);
                return nonprofit;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Nonprofit: UserId={UserId}", i_userId);
                throw;
            }
        }

        public async Task<Gainer> UpdateGainerProfileAsync(Guid i_userId, GainerProfileUpdateDTO i_updateDto)
        {
            r_logger.LogInformation("Updating Gainer profile: UserId={UserId}", i_userId);

            try
            {
                var gainer = await GetGainerByIdAsync(i_userId);
                if (gainer == null)
                {
                    r_logger.LogWarning("Gainer not found for update: UserId={UserId}", i_userId);
                    throw new KeyNotFoundException($"Gainer with ID {i_userId} not found");
                }

                // Update base user properties
                gainer.FullName = i_updateDto.FullName;
                gainer.Biography = i_updateDto.Biography;
                gainer.FacebookPageURL = i_updateDto.FacebookPageURL;
                gainer.LinkedInURL = i_updateDto.LinkedInURL;
                gainer.GitHubURL = i_updateDto.GitHubURL;
                gainer.GitHubUsername = i_updateDto.GitHubUsername;  // Add GitHub username update
                gainer.ProfilePictureURL = i_updateDto.ProfilePictureURL;

                // Update Gainer-specific properties
                gainer.EducationStatus = i_updateDto.EducationStatus;
                gainer.AreasOfInterest = i_updateDto.AreasOfInterest;   // we get it as List<string> so we need to think how to take it

                await r_DbContext.SaveChangesAsync();
                
                r_logger.LogInformation("Successfully updated Gainer profile: UserId={UserId}", i_userId);
                return gainer;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error updating Gainer profile: UserId={UserId}", i_userId);
                throw;
            }
        }

        public async Task<Mentor> UpdateMentorProfileAsync(Guid userId, MentorProfileUpdateDTO i_updateDto)
        {
            r_logger.LogInformation("Updating Mentor profile: UserId={UserId}", userId);

            try
            {
                var mentor = await GetMentorByIdAsync(userId);
                if (mentor == null)
                {
                    r_logger.LogWarning("Mentor not found for update: UserId={UserId}", userId);
                    throw new KeyNotFoundException($"Mentor with ID {userId} not found");
                }

                // Update base user properties
                mentor.FullName = i_updateDto.FullName;
                mentor.Biography = i_updateDto.Biography;
                mentor.FacebookPageURL = i_updateDto.FacebookPageURL;
                mentor.LinkedInURL = i_updateDto.LinkedInURL;
                mentor.GitHubURL = i_updateDto.GitHubURL;
                mentor.GitHubUsername = i_updateDto.GitHubUsername;  // Add GitHub username update
                mentor.ProfilePictureURL = i_updateDto.ProfilePictureURL;

                // Update Mentor-specific properties
                mentor.YearsOfExperience = i_updateDto.YearsOfExperience;
                mentor.AreaOfExpertise = i_updateDto.AreaOfExpertise;

                // Update TechExpertise
                if (mentor.TechExpertise == null)
                {
                    r_logger.LogDebug("Mentor does not have TechExpertise. Creating new TechExpertise for Mentor: UserId={UserId}", userId);
                    mentor.TechExpertise = new TechExpertise
                    {
                        User = mentor // Set the required 'User' property to the current mentor instance
                    };
                }
                mentor.TechExpertise.ProgrammingLanguages = i_updateDto.TechExpertise.ProgrammingLanguages;
                mentor.TechExpertise.Technologies = i_updateDto.TechExpertise.Technologies;
                mentor.TechExpertise.Tools = i_updateDto.TechExpertise.Tools;

                await r_DbContext.SaveChangesAsync();
                
                r_logger.LogInformation("Successfully updated Mentor profile: UserId={UserId}", userId);
                return mentor;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error updating Mentor profile: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task<NonprofitOrganization> UpdateNonprofitProfileAsync(Guid userId, NonprofitProfileUpdateDTO updateDto)
        {
            r_logger.LogInformation("Updating Nonprofit profile: UserId={UserId}", userId);

            try
            {
                var nonprofit = await GetNonprofitByIdAsync(userId);
                if (nonprofit == null)
                {
                    r_logger.LogWarning("Nonprofit not found for update: UserId={UserId}", userId);
                    throw new KeyNotFoundException($"Nonprofit with ID {userId} not found");
                }

                // Update base user properties
                nonprofit.FullName = updateDto.FullName;
                nonprofit.Biography = updateDto.Biography;
                nonprofit.FacebookPageURL = updateDto.FacebookPageURL;
                nonprofit.LinkedInURL = updateDto.LinkedInURL;
                nonprofit.GitHubURL = updateDto.GitHubURL;
                nonprofit.GitHubUsername = updateDto.GitHubUsername;  // Add GitHub username update
                nonprofit.ProfilePictureURL = updateDto.ProfilePictureURL;

                // Update Nonprofit-specific properties
                nonprofit.WebsiteUrl = updateDto.WebsiteUrl;

                // Update NonprofitExpertise
                if (nonprofit.NonprofitExpertise == null)
                {
                    r_logger.LogDebug("NonprofitExpertise is null for Nonprofit: UserId={UserId}. Creating new NonprofitExpertise.", userId);
                    nonprofit.NonprofitExpertise = new NonprofitExpertise
                    {
                        User = nonprofit, 
                        FieldOfWork = updateDto.NonprofitExpertise.FieldOfWork, 
                        MissionStatement = updateDto.NonprofitExpertise.MissionStatement 
                    };
                    r_logger.LogDebug("Created new NonprofitExpertise for Nonprofit: UserId={UserId}", userId);
                }
                else
                {
                    nonprofit.NonprofitExpertise.FieldOfWork = updateDto.NonprofitExpertise.FieldOfWork;
                    nonprofit.NonprofitExpertise.MissionStatement = updateDto.NonprofitExpertise.MissionStatement;
                }

                await r_DbContext.SaveChangesAsync();
                
                r_logger.LogInformation("Successfully updated Nonprofit profile: UserId={UserId}", userId);
                return nonprofit;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error updating Nonprofit profile: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<TechExpertise>> GetGainerExpertiseAsync(Guid userId)
        {
            r_logger.LogInformation("Getting Gainer expertise: UserId={UserId}", userId);

            try
            {
                var gainer = await GetGainerByIdAsync(userId);
                var expertise = gainer?.TechExpertise != null
                    ? new List<TechExpertise> { gainer.TechExpertise }
                    : Enumerable.Empty<TechExpertise>();

                r_logger.LogInformation("Retrieved Gainer expertise: UserId={UserId}, Count={Count}", userId, expertise.Count());
                return expertise;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Gainer expertise: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<TechExpertise>> GetMentorExpertiseAsync(Guid userId)
        {
            r_logger.LogInformation("Getting Mentor expertise: UserId={UserId}", userId);

            try
            {
                var mentor = await GetMentorByIdAsync(userId);
                var expertise = mentor?.TechExpertise != null
                    ? new List<TechExpertise> { mentor.TechExpertise }
                    : Enumerable.Empty<TechExpertise>();

                r_logger.LogInformation("Retrieved Mentor expertise: UserId={UserId}, Count={Count}", userId, expertise.Count());
                return expertise;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Mentor expertise: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<NonprofitExpertise>> GetNonprofitExpertiseAsync(Guid userId)
        {
            r_logger.LogInformation("Getting Nonprofit expertise: UserId={UserId}", userId);

            try
            {
                var nonprofit = await GetNonprofitByIdAsync(userId);
                var expertise = nonprofit?.NonprofitExpertise != null
                    ? new List<NonprofitExpertise> { nonprofit.NonprofitExpertise }
                    : Enumerable.Empty<NonprofitExpertise>();

                r_logger.LogInformation("Retrieved Nonprofit expertise: UserId={UserId}, Count={Count}", userId, expertise.Count());
                return expertise;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Nonprofit expertise: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task<TechExpertise> AddExpertiseToGainerAsync(Guid userId, AddTechExpertiseDto expertiseDto)
        {
            r_logger.LogInformation("Adding expertise to Gainer: UserId={UserId}", userId);

            try
            {
                var gainer = await GetGainerByIdAsync(userId);
                if (gainer == null)
                    throw new KeyNotFoundException($"Gainer with ID {userId} not found");

                // Ensure TechExpertise is initialized before adding expertise  
                if (gainer.TechExpertise == null)
                {
                    r_logger.LogDebug("Gainer does not have TechExpertise. Creating new TechExpertise for Gainer: UserId={UserId}", userId);
                    gainer.TechExpertise = new TechExpertise
                    {
                        User = gainer, // Set the required 'User' property to the current gainer instance  
                        ProgrammingLanguages = new List<string>(),
                        Technologies = new List<string>(),
                        Tools = new List<string>()
                    };
                }

                // Clear existing and add new expertise (replaces instead of appending)
                gainer.TechExpertise.ProgrammingLanguages.Clear();
                gainer.TechExpertise.ProgrammingLanguages.AddRange(expertiseDto.ProgrammingLanguages ?? new List<string>());
                
                gainer.TechExpertise.Technologies.Clear();
                gainer.TechExpertise.Technologies.AddRange(expertiseDto.Technologies ?? new List<string>());
                
                gainer.TechExpertise.Tools.Clear();
                gainer.TechExpertise.Tools.AddRange(expertiseDto.Tools ?? new List<string>());

                await r_DbContext.SaveChangesAsync();

                r_logger.LogInformation("Successfully updated expertise for Gainer: UserId={UserId}", userId);
                return gainer.TechExpertise;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error updating expertise for Gainer: UserId={UserId}", userId);
                throw;
            }
        }



        // checked the methods above , from here need to go over






        public async Task<TechExpertise> AddExpertiseToMentorAsync(Guid userId, AddTechExpertiseDto expertiseDto)
        {
            r_logger.LogInformation("Adding expertise to Mentor: UserId={UserId}", userId);

            try
            {
                var mentor = await GetMentorByIdAsync(userId);
                if (mentor == null)
                    throw new KeyNotFoundException($"Mentor with ID {userId} not found");

                // Ensure TechExpertise is initialized before adding expertise  
                if (mentor.TechExpertise == null)
                {
                    r_logger.LogDebug("Mentor does not have TechExpertise. Creating new TechExpertise for Mentor: UserId={UserId}", userId);
                    mentor.TechExpertise = new TechExpertise
                    {
                        User = mentor, // Set the required 'User' property to the current mentor instance  
                        ProgrammingLanguages = new List<string>(),
                        Technologies = new List<string>(),
                        Tools = new List<string>()
                    };
                }

                // Clear existing and add new expertise (replaces instead of appending)
                mentor.TechExpertise.ProgrammingLanguages.Clear();
                mentor.TechExpertise.ProgrammingLanguages.AddRange(expertiseDto.ProgrammingLanguages ?? new List<string>());
                
                mentor.TechExpertise.Technologies.Clear();
                mentor.TechExpertise.Technologies.AddRange(expertiseDto.Technologies ?? new List<string>());
                
                mentor.TechExpertise.Tools.Clear();
                mentor.TechExpertise.Tools.AddRange(expertiseDto.Tools ?? new List<string>());

                await r_DbContext.SaveChangesAsync();

                r_logger.LogInformation("Successfully updated expertise for Mentor: UserId={UserId}", userId);
                return mentor.TechExpertise;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error updating expertise for Mentor: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task<NonprofitExpertise> AddExpertiseToNonprofitAsync(Guid userId, AddNonprofitExpertiseDto expertiseDto)
        {
            r_logger.LogInformation("Adding expertise to Nonprofit: UserId={UserId}, FieldOfWork={FieldOfWork}", userId, expertiseDto.FieldOfWork);

            try
            {
                var nonprofit = await GetNonprofitByIdAsync(userId);
                if (nonprofit == null)
                    throw new KeyNotFoundException($"Nonprofit with ID {userId} not found");

                // Ensure NonprofitExpertise is initialized before adding expertise  
                if (nonprofit.NonprofitExpertise == null)
                {
                    r_logger.LogDebug("Nonprofit does not have NonprofitExpertise. Creating new NonprofitExpertise for Nonprofit: UserId={UserId}", userId);
                    nonprofit.NonprofitExpertise = new NonprofitExpertise
                    {
                        User = nonprofit, // Set the required 'User' property to the current nonprofit instance  
                        FieldOfWork = expertiseDto.FieldOfWork,
                        MissionStatement = expertiseDto.MissionStatement
                    };
                }
                else
                {
                    // Update existing NonprofitExpertise with new data (replaces instead of appending)
                    nonprofit.NonprofitExpertise.FieldOfWork = expertiseDto.FieldOfWork;
                    nonprofit.NonprofitExpertise.MissionStatement = expertiseDto.MissionStatement;
                }

                await r_DbContext.SaveChangesAsync();

                r_logger.LogInformation("Successfully updated expertise for Nonprofit: UserId={UserId}, FieldOfWork={FieldOfWork}", userId, expertiseDto.FieldOfWork);
                return nonprofit.NonprofitExpertise;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error adding expertise to Nonprofit: UserId={UserId}, FieldOfWork={FieldOfWork}", userId, expertiseDto.MissionStatement);
                throw;
            }
        }

        public async Task<IEnumerable<UserAchievement>> GetGainerAchievementsAsync(Guid userId)
        {
            r_logger.LogInformation("Getting Gainer achievements: UserId={UserId}", userId);

            try
            {
                var achievements = await r_DbContext.UserAchievements
                    .Include(ua => ua.AchievementTemplate)
                    .Where(a => a.UserId == userId)
                    .ToListAsync();

                r_logger.LogInformation("Retrieved Gainer achievements: UserId={UserId}, Count={Count}", userId, achievements.Count);
                return achievements;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Gainer achievements: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<UserAchievement>> GetMentorAchievementsAsync(Guid userId)
        {
            r_logger.LogInformation("Getting Mentor achievements: UserId={UserId}", userId);

            try
            {
                var achievements = await r_DbContext.UserAchievements
                    .Include(ua => ua.AchievementTemplate)
                    .Where(a => a.UserId == userId)
                    .ToListAsync();

                r_logger.LogInformation("Retrieved Mentor achievements: UserId={UserId}, Count={Count}", userId, achievements.Count);
                return achievements;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Mentor achievements: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<UserAchievement>> GetNonprofitAchievementsAsync(Guid userId)
        {
            r_logger.LogInformation("Getting Nonprofit achievements: UserId={UserId}", userId);

            try
            {
                var achievements = await r_DbContext.UserAchievements
                    .Include(ua => ua.AchievementTemplate)
                    .Where(a => a.UserId == userId)
                    .ToListAsync();

                r_logger.LogInformation("Retrieved Nonprofit achievements: UserId={UserId}, Count={Count}", userId, achievements.Count);
                return achievements;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Nonprofit achievements: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task<UserAchievement> AddAchievementToGainerAsync(Guid userId, Guid achievementTemplateId)
        {
            r_logger.LogInformation("Adding achievement to Gainer: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", userId, achievementTemplateId);

            try
            {
                var gainer = await GetGainerByIdAsync(userId);
                if (gainer == null)
                {
                    r_logger.LogWarning("Gainer not found: UserId={UserId}", userId);
                    throw new KeyNotFoundException($"Gainer with ID {userId} not found");
                }

                var achievementTemplate = await r_DbContext.AchievementTemplates.FindAsync(achievementTemplateId);
                if (achievementTemplate == null)
                {
                    r_logger.LogWarning("Achievement template not found: AchievementTemplateId={AchievementTemplateId}", achievementTemplateId);
                    throw new KeyNotFoundException($"Achievement template with ID {achievementTemplateId} not found");
                }

                // Prevent duplicate (UserId, AchievementTemplateId)
                var existing = await r_DbContext.UserAchievements
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.AchievementTemplateId == achievementTemplateId);
                if (existing != null)
                {
                    r_logger.LogInformation("Achievement already exists for Gainer. No changes: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", userId, achievementTemplateId);
                    return existing;
                }

                var achievement = new UserAchievement
                {
                    UserId = userId,
                    AchievementTemplateId = achievementTemplateId,
                    EarnedAtUtc = DateTime.UtcNow,
                    User = gainer,
                    AchievementTemplate = achievementTemplate,
                    EarnedDetails = $"Earned '{achievementTemplate.Title}' on {DateTime.UtcNow:yyyy-MM-dd}"
                };

                // Add to DbContext explicitly to ensure proper tracking and ID generation
                r_DbContext.UserAchievements.Add(achievement);
                await r_DbContext.SaveChangesAsync();

                r_logger.LogInformation("Successfully added achievement to Gainer: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", userId, achievementTemplateId);
                return achievement;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error adding achievement to Gainer: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", userId, achievementTemplateId);
                throw;
            }
        }

        public async Task<UserAchievement> AddAchievementToMentorAsync(Guid userId, Guid achievementTemplateId)
        {
            r_logger.LogInformation("Adding achievement to Mentor: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", userId, achievementTemplateId);

            try
            {
                var mentor = await GetMentorByIdAsync(userId);
                if (mentor == null)
                {
                    r_logger.LogWarning("Mentor not found: UserId={UserId}", userId);
                    throw new KeyNotFoundException($"Mentor with ID {userId} not found");
                }

                var achievementTemplate = await r_DbContext.AchievementTemplates.FindAsync(achievementTemplateId);
                if (achievementTemplate == null)
                {
                    r_logger.LogWarning("Achievement template not found: AchievementTemplateId={AchievementTemplateId}", achievementTemplateId);
                    throw new KeyNotFoundException($"Achievement template with ID {achievementTemplateId} not found");
                }

                // Prevent duplicate (UserId, AchievementTemplateId)
                var existing = await r_DbContext.UserAchievements
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.AchievementTemplateId == achievementTemplateId);
                if (existing != null)
                {
                    r_logger.LogInformation("Achievement already exists for Mentor. No changes: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", userId, achievementTemplateId);
                    return existing;
                }

                var achievement = new UserAchievement
                {
                    UserId = userId,
                    AchievementTemplateId = achievementTemplateId,
                    EarnedAtUtc = DateTime.UtcNow,
                    User = mentor,
                    AchievementTemplate = achievementTemplate,
                    EarnedDetails = $"Earned '{achievementTemplate.Title}' on {DateTime.UtcNow:yyyy-MM-dd}"
                };

                // Add to DbContext explicitly to ensure proper tracking and ID generation
                r_DbContext.UserAchievements.Add(achievement);
                await r_DbContext.SaveChangesAsync();

                r_logger.LogInformation("Successfully added achievement to Mentor: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", userId, achievementTemplateId);
                return achievement;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error adding achievement to Mentor: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", userId, achievementTemplateId);
                throw;
            }
        }

        public async Task<UserAchievement> AddAchievementToNonprofitAsync(Guid userId, Guid achievementTemplateId)
        {
            r_logger.LogInformation("Adding achievement to Nonprofit: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", userId, achievementTemplateId);

            try
            {
                var nonprofit = await GetNonprofitByIdAsync(userId);
                if (nonprofit == null)
                {
                    r_logger.LogWarning("Nonprofit not found: UserId={UserId}", userId);
                    throw new KeyNotFoundException($"Nonprofit with ID {userId} not found");
                }

                var achievementTemplate = await r_DbContext.AchievementTemplates.FindAsync(achievementTemplateId);
                if (achievementTemplate == null)
                {
                    r_logger.LogWarning("Achievement template not found: AchievementTemplateId={AchievementTemplateId}", achievementTemplateId);
                    throw new KeyNotFoundException($"Achievement template with ID {achievementTemplateId} not found");
                }

                // Prevent duplicate (UserId, AchievementTemplateId)
                var existing = await r_DbContext.UserAchievements
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.AchievementTemplateId == achievementTemplateId);
                if (existing != null)
                {
                    r_logger.LogInformation("Achievement already exists for Nonprofit. No changes: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", userId, achievementTemplateId);
                    return existing;
                }

                var achievement = new UserAchievement
                {
                    UserId = userId,
                    AchievementTemplateId = achievementTemplateId,
                    EarnedAtUtc = DateTime.UtcNow,
                    User = nonprofit,
                    AchievementTemplate = achievementTemplate,
                    EarnedDetails = $"Earned '{achievementTemplate.Title}' on {DateTime.UtcNow:yyyy-MM-dd}"
                };

                // Add to DbContext explicitly to ensure proper tracking and ID generation
                r_DbContext.UserAchievements.Add(achievement);
                await r_DbContext.SaveChangesAsync();

                r_logger.LogInformation("Successfully added achievement to Nonprofit: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", userId, achievementTemplateId);
                return achievement;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error adding achievement to Nonprofit: UserId={UserId}, AchievementTemplateId={AchievementTemplateId}", userId, achievementTemplateId);
                throw;
            }
        }

        public async Task<IEnumerable<UserProject>> GetGainerProjectHistoryAsync(Guid userId)
        {
            r_logger.LogInformation("Getting Gainer project history: UserId={UserId}", userId);

            try
            {
                var gainer = await GetGainerByIdAsync(userId);
                var projects = gainer?.ParticipatedProjects ?? Enumerable.Empty<UserProject>();
                r_logger.LogInformation("Retrieved Gainer project history: UserId={UserId}, Count={Count}", userId, projects.Count());
                return projects;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Gainer project history: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<UserProject>> GetMentorProjectHistoryAsync(Guid userId)
        {
            r_logger.LogInformation("Getting Mentor project history: UserId={UserId}", userId);

            try
            {
                var mentor = await GetMentorByIdAsync(userId);
                var projects = mentor?.MentoredProjects ?? Enumerable.Empty<UserProject>();
                r_logger.LogInformation("Retrieved Mentor project history: UserId={UserId}, Count={Count}", userId, projects.Count());
                return projects;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Mentor project history: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<UserProject>> GetNonprofitProjectHistoryAsync(Guid userId)
        {
            r_logger.LogInformation("Getting Nonprofit project history: UserId={UserId}", userId);

            try
            {
                var nonprofit = await GetNonprofitByIdAsync(userId);
                var projects = nonprofit?.OwnedProjects ?? Enumerable.Empty<UserProject>();
                r_logger.LogInformation("Retrieved Nonprofit project history: UserId={UserId}, Count={Count}", userId, projects.Count());
                return projects;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Nonprofit project history: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task<List<UserProject>> GetNonprofitOwnedProjectsAsync(Guid nonprofitUserId)
        {
            r_logger.LogInformation("Getting Nonprofit owned projects: NonprofitUserId={NonprofitUserId}", nonprofitUserId);

            try
            {
                var projects = await r_DbContext.Projects
                    .Where(p => p.OwningOrganizationUserId == nonprofitUserId)
                    .ToListAsync();

                r_logger.LogInformation("Retrieved Nonprofit owned projects: NonprofitUserId={NonprofitUserId}, Count={Count}", nonprofitUserId, projects.Count);
                return projects;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving Nonprofit owned projects: NonprofitUserId={NonprofitUserId}", nonprofitUserId);
                throw;
            }
        }

        public async Task<List<UserAchievement>> GetUserAchievementsAsync(Guid userId)
        {
            r_logger.LogInformation("Getting user achievements: UserId={UserId}", userId);

            try
            {
                var achievements = await r_DbContext.UserAchievements
                    .Include(ua => ua.AchievementTemplate)
                    .Where(a => a.UserId == userId)
                    .ToListAsync();

                r_logger.LogInformation("Retrieved user achievements: UserId={UserId}, Count={Count}", userId, achievements.Count);
                return achievements;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error retrieving user achievements: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Gainer>> SearchGainersAsync(string searchTerm)
        {
            r_logger.LogInformation("Searching Gainers: SearchTerm={SearchTerm}", searchTerm);

            try
            {
                var results = await r_DbContext.Gainers
                    .Include(g => g.TechExpertise)
                    .Include(g => g.Achievements)
                    .Where(g => (g.FullName != null && g.FullName.Contains(searchTerm)) || 
                               (g.Biography != null && g.Biography.Contains(searchTerm)) ||
                               (g.AreasOfInterest != null && g.AreasOfInterest.Any(area => area.Contains(searchTerm))) ||
                               (g.TechExpertise != null && 
                                (g.TechExpertise.ProgrammingLanguages != null && g.TechExpertise.ProgrammingLanguages.Any(lang => lang.Contains(searchTerm)) ||
                                 g.TechExpertise.Technologies != null && g.TechExpertise.Technologies.Any(tech => tech.Contains(searchTerm)) ||
                                 g.TechExpertise.Tools != null && g.TechExpertise.Tools.Any(tool => tool.Contains(searchTerm)))))
                    .ToListAsync();

                r_logger.LogInformation("Successfully searched Gainers: SearchTerm={SearchTerm}, ResultsCount={ResultsCount}", searchTerm, results.Count);
                return results;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error searching Gainers: SearchTerm={SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<Mentor>> SearchMentorsAsync(string searchTerm)
        {
            r_logger.LogInformation("Searching Mentors: SearchTerm={SearchTerm}", searchTerm);

            try
            {
                var results = await r_DbContext.Mentors
                    .Include(m => m.TechExpertise)
                    .Include(m => m.Achievements)
                    .Where(m => (m.FullName != null && m.FullName.Contains(searchTerm)) || 
                               (m.Biography != null && m.Biography.Contains(searchTerm)) ||
                               (m.AreaOfExpertise != null && m.AreaOfExpertise.Contains(searchTerm)) ||
                               (m.TechExpertise != null && 
                                (m.TechExpertise.ProgrammingLanguages != null && m.TechExpertise.ProgrammingLanguages.Any(lang => lang.Contains(searchTerm)) ||
                                 m.TechExpertise.Technologies != null && m.TechExpertise.Technologies.Any(tech => tech.Contains(searchTerm)) ||
                                 m.TechExpertise.Tools != null && m.TechExpertise.Tools.Any(tool => tool.Contains(searchTerm)))))
                    .ToListAsync();

                r_logger.LogInformation("Successfully searched Mentors: SearchTerm={SearchTerm}, ResultsCount={ResultsCount}", searchTerm, results.Count);
                return results;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error searching Mentors: SearchTerm={SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<NonprofitOrganization>> SearchNonprofitsAsync(string searchTerm)
        {
            r_logger.LogInformation("Searching Nonprofits: SearchTerm={SearchTerm}", searchTerm);

            try
            {
                var results = await r_DbContext.Nonprofits
                    .Include(n => n.NonprofitExpertise)
                    .Include(n => n.Achievements)
                    .Where(n => (n.FullName != null && n.FullName.Contains(searchTerm)) || 
                               (n.Biography != null && n.Biography.Contains(searchTerm)) ||
                               (n.WebsiteUrl != null && n.WebsiteUrl.Contains(searchTerm)) ||
                               (n.NonprofitExpertise != null && 
                                (n.NonprofitExpertise.FieldOfWork != null && n.NonprofitExpertise.FieldOfWork.Contains(searchTerm) ||
                                 n.NonprofitExpertise.MissionStatement != null && n.NonprofitExpertise.MissionStatement.Contains(searchTerm))))
                    .ToListAsync();

                r_logger.LogInformation("Successfully searched Nonprofits: SearchTerm={SearchTerm}, ResultsCount={ResultsCount}", searchTerm, results.Count);
                return results;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error searching Nonprofits: SearchTerm={SearchTerm}", searchTerm);
                throw;
            }
        }

        
    }
}
