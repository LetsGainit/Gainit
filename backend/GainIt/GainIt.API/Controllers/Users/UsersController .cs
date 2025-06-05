using GainIt.API.DTOs.ViewModels.Users;
using GainIt.API.Models.Users.Gainers;
using GainIt.API.Models.Users.Mentors;
using GainIt.API.Models.Users.Nonprofits;
using GainIt.API.Services.Users.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GainIt.API.Controllers.Users
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserProfileService _userProfileService;

        public UsersController(IUserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        /// <summary>
        /// Get full Gainer profile by ID.
        /// </summary>
        [HttpGet("gainer/{id}/profile")]
        public async Task<IActionResult> GetGainerProfile(Guid id)
        {
            Gainer gainer = await _userProfileService.GetGainerByIdAsync(id);
            if (gainer == null) return NotFound();

            FullGainerViewModel gainerViewModel = new FullGainerViewModel(gainer);
            return Ok(gainerViewModel);
        }

        /// <summary>
        /// Get full Mentor profile by ID.
        /// </summary>
        [HttpGet("mentor/{id}/profile")]
        public async Task<IActionResult> GetMentorProfile(Guid id)
        {
            Mentor mentor = await _userProfileService.GetMentorByIdAsync(id);
            if (mentor == null) return NotFound();

            FullMentorViewModel mentorViewModel = new FullMentorViewModel(mentor);
            return Ok(mentorViewModel);
        }

        /// <summary>
        /// Get full Nonprofit profile by ID.
        /// </summary>
        [HttpGet("nonprofit/{id}/profile")]
        public async Task<IActionResult> GetNonprofitProfile(Guid id)
        {
            NonprofitOrganization nonprofit = await _userProfileService.GetNonprofitByIdAsync(id);
            if (nonprofit == null)
            { 
                return NotFound(); 
            }

            FullNonprofitViewModel nonprofitViewModel = new FullNonprofitViewModel(nonprofit);
            return Ok(nonprofitViewModel);
        }
    }
}

