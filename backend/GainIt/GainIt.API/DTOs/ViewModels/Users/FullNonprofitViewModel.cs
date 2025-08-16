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

        public FullNonprofitViewModel(NonprofitOrganization nonprofit, List<UserProject>? projects, bool includeProjects = true) : base(nonprofit)
        {
            WebsiteUrl = nonprofit.WebsiteUrl;
            NonprofitExpertise = new NonprofitExpertiseViewModel(nonprofit.NonprofitExpertise);

            // Only include projects if explicitly requested and provided
            OwnedProjects = (includeProjects && projects != null)
                ? projects.Select(UserProject => new ConciseUserProjectViewModel(UserProject, nonprofit.UserId)).ToList()
                : new List<ConciseUserProjectViewModel>();
        }
    }
}
