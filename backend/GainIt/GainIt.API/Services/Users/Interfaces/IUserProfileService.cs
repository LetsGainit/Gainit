using GainIt.API.DTOs.ViewModels.Users;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users.Nonprofits;

namespace GainIt.API.Services.Users.Interfaces
{
    public interface IUserProfileService
    {
        Task<Gainer> GetGainerByIdAsync(Guid gainerId);
        Task<Mentor> GetMentorByIdAsync(Guid mentorId);
        Task<NonprofitOrganization> GetNonprofitByIdAsync(Guid nonprofitId);
    }
}
