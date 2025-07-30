using GainIt.API.DTOs.ViewModels.Expertise;
using GainIt.API.DTOs.ViewModels.Projects;
using GainIt.API.Models.Users.Nonprofits;
using GainIt.API.Models.Projects;

namespace GainIt.API.DTOs.ViewModels.Users
{
    public class FullNonprofitViewModel : BaseFullUserViewModel
    {
        public string WebsiteUrl { get; set; }
        public NonprofitExpertiseViewModel NonprofitExpertise { get; set; }
        public List<ConciseUserProjectViewModel> OwnedProjects { get; set; } = new List<ConciseUserProjectViewModel>(); 

        public FullNonprofitViewModel(NonprofitOrganization nonprofit, List<UserProject> projects) : base(nonprofit)
        {
            WebsiteUrl = nonprofit.WebsiteUrl;
            NonprofitExpertise = new NonprofitExpertiseViewModel(nonprofit.NonprofitExpertise);

            OwnedProjects = projects
                .Select(UserProject => new ConciseUserProjectViewModel(UserProject, nonprofit.UserId))
                .ToList();
        }
    }
}
