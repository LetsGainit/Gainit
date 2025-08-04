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
                var mentor = await r_DbContext.Mentors
                    .Include(m => m.TechExpertise)
                    .Include(m => m.MentoredProjects)
                    .Include(m => m.Achievements)
                    .FirstOrDefaultAsync(m => m.UserId == i_userId);

                if (mentor == null)
                {
                    r_logger.LogWarning("Mentor not found: UserId={UserId}", i_userId);
                    throw new KeyNotFoundException($"Mentor with ID {i_userId} not found");
                }

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

        public async Task<TechExpertise> AddExpertiseToGainerAsync(Guid userId, TechExpertise expertise)
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

                // Add expertise details to the respective lists  
                gainer.TechExpertise.ProgrammingLanguages.AddRange(expertise.ProgrammingLanguages);
                gainer.TechExpertise.Technologies.AddRange(expertise.Technologies);
                gainer.TechExpertise.Tools.AddRange(expertise.Tools);

                await r_DbContext.SaveChangesAsync();

                r_logger.LogInformation("Successfully added expertise to Gainer: UserId={UserId}", userId);
                return gainer.TechExpertise;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error adding expertise to Gainer: UserId={UserId}", userId);
                throw;
            }
        }



        // checked the methods above , from here need to go over






        public async Task<TechExpertise> AddExpertiseToMentorAsync(Guid userId, TechExpertise expertise)
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

                // Add expertise details to the respective lists  
                mentor.TechExpertise.ProgrammingLanguages.AddRange(expertise.ProgrammingLanguages);
                mentor.TechExpertise.Technologies.AddRange(expertise.Technologies);
                mentor.TechExpertise.Tools.AddRange(expertise.Tools);

                await r_DbContext.SaveChangesAsync();

                r_logger.LogInformation("Successfully added expertise to Mentor: UserId={UserId}", userId);
                return mentor.TechExpertise;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error adding expertise to Mentor: UserId={UserId}", userId);
                throw;
            }
        }

        public async Task<NonprofitExpertise> AddExpertiseToNonprofitAsync(Guid userId, NonprofitExpertise expertise)
        {
            r_logger.LogInformation("Adding expertise to Nonprofit: UserId={UserId}, FieldOfWork={FieldOfWork}", userId, expertise.FieldOfWork);

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
                        FieldOfWork = expertise.FieldOfWork,
                        MissionStatement = expertise.MissionStatement
                    };
                }
                else
                {
                    // Update existing NonprofitExpertise with new data
                    nonprofit.NonprofitExpertise.FieldOfWork = expertise.FieldOfWork;
                    nonprofit.NonprofitExpertise.MissionStatement = expertise.MissionStatement;
                }

                await r_DbContext.SaveChangesAsync();

                r_logger.LogInformation("Successfully added expertise to Nonprofit: UserId={UserId}, FieldOfWork={FieldOfWork}", userId, expertise.FieldOfWork);
                return nonprofit.NonprofitExpertise;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error adding expertise to Nonprofit: UserId={UserId}, FieldOfWork={FieldOfWork}", userId, expertise.FieldOfWork);
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

                var achievement = new UserAchievement
                {
                    UserId = userId,
                    AchievementTemplateId = achievementTemplateId,
                    EarnedAtUtc = DateTime.UtcNow,
                    User = gainer,
                    AchievementTemplate = achievementTemplate
                };

                gainer.Achievements.Add(achievement);
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

                var achievement = new UserAchievement
                {
                    UserId = userId,
                    AchievementTemplateId = achievementTemplateId,
                    EarnedAtUtc = DateTime.UtcNow,
                    User = mentor,
                    AchievementTemplate = achievementTemplate
                };

                mentor.Achievements.Add(achievement);
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

                var achievement = new UserAchievement
                {
                    UserId = userId,
                    AchievementTemplateId = achievementTemplateId,
                    EarnedAtUtc = DateTime.UtcNow,
                    User = nonprofit,
                    AchievementTemplate = achievementTemplate
                };

                nonprofit.Achievements.Add(achievement);
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

        public Task<IEnumerable<Gainer>> SearchGainersAsync(string searchTerm)
        {
            r_logger.LogWarning("SearchGainersAsync not implemented: SearchTerm={SearchTerm}", searchTerm);
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Mentor>> SearchMentorsAsync(string searchTerm)
        {
            r_logger.LogWarning("SearchMentorsAsync not implemented: SearchTerm={SearchTerm}", searchTerm);
            throw new NotImplementedException();
        }

        public Task<IEnumerable<NonprofitOrganization>> SearchNonprofitsAsync(string searchTerm)
        {
            r_logger.LogWarning("SearchNonprofitsAsync not implemented: SearchTerm={SearchTerm}", searchTerm);
            throw new NotImplementedException();
        }

        /*public async Task<IEnumerable<Gainer>> SearchGainersAsync(string searchTerm)
        {
            return await _dbContext.Gainers
                .Include(g => g.TechExpertise)
                .Where(g => g.FullName.Contains(searchTerm) || 
                           g.Biography.Contains(searchTerm) ||
                           g.TechExpertise.Any(e => e.Name.Contains(searchTerm)))
                .ToListAsync();
        }

        public async Task<IEnumerable<Mentor>> SearchMentorsAsync(string searchTerm)
        {
            return await _dbContext.Mentors
                .Include(m => m.TechExpertise)
                .Where(m => m.FullName.Contains(searchTerm) || 
                           m.Biography.Contains(searchTerm) ||
                           m.TechExpertise.Any(e => e.Name.Contains(searchTerm)))
                .ToListAsync();
        }

        public async Task<IEnumerable<NonprofitOrganization>> SearchNonprofitsAsync(string searchTerm)
        {
            return await _dbContext.Nonprofits
                .Include(n => n.NonprofitExpertise)
                .Where(n => n.FullName.Contains(searchTerm) || 
                           n.Biography.Contains(searchTerm) ||
                           n.NonprofitExpertise.Any(e => e.Name.Contains(searchTerm)))
                .ToListAsync();
        }*/



        ////////////////////   the stats sevice? ///////////////////////////////////////////////////

        /*public async Task<GainerStats> GetGainerStatsAsync(Guid userId)
        {
            var gainer = await GetGainerByIdAsync(userId);
            if (gainer == null)
                throw new KeyNotFoundException($"Gainer with ID {userId} not found");

            return new GainerStats
            {
                TotalProjects = gainer.ParticipatedProjects.Count,
                CompletedProjects = gainer.ParticipatedProjects.Count(p => p.ProjectStatus == eProjectStatus.Completed),
                TotalAchievements = gainer.Achievements.Count,
                ExpertiseCount = gainer.TechExpertise.Count
            };
        }

        public async Task<MentorStats> GetMentorStatsAsync(Guid userId)
        {
            var mentor = await GetMentorByIdAsync(userId);
            if (mentor == null)
                throw new KeyNotFoundException($"Mentor with ID {userId} not found");

            return new MentorStats
            {
                TotalMentoredProjects = mentor.MentoredProjects.Count,
                CompletedMentoredProjects = mentor.MentoredProjects.Count(p => p.ProjectStatus == eProjectStatus.Completed),
                TotalAchievements = mentor.Achievements.Count,
                ExpertiseCount = mentor.TechExpertise.Count
            };
        }

        public async Task<NonprofitStats> GetNonprofitStatsAsync(Guid userId)
        {
            var nonprofit = await GetNonprofitByIdAsync(userId);
            if (nonprofit == null)
                throw new KeyNotFoundException($"Nonprofit with ID {userId} not found");

            return new NonprofitStats
            {
                TotalOwnedProjects = nonprofit.OwnedProjects.Count,
                CompletedProjects = nonprofit.OwnedProjects.Count(p => p.ProjectStatus == eProjectStatus.Completed),
                TotalAchievements = nonprofit.Achievements.Count,
                ExpertiseCount = nonprofit.NonprofitExpertise.Count
            };
        }*/
    }
}
