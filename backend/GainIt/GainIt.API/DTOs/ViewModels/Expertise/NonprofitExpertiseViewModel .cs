using GainIt.API.Models.Users.Expertise;

namespace GainIt.API.DTOs.ViewModels.Expertise
{
    public class NonprofitExpertiseViewModel
    {
        public string FieldOfWork { get; set; }
        public string MissionStatement { get; set; }

        public NonprofitExpertiseViewModel(NonprofitExpertise i_NonprofitExpertise)
        {
            FieldOfWork = i_NonprofitExpertise.FieldOfWork;
            MissionStatement = i_NonprofitExpertise.MissionStatement;
        }
    }
}
