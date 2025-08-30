using GainIt.API.Models.Users.Expertise;

namespace GainIt.API.DTOs.ViewModels.Expertise
{
    public class TechExpertiseViewModel
    {
        public List<string> ProgrammingLanguages { get; set; } = new List<string>();
        public List<string> Technologies { get; set; } = new List<string>();
        public List<string> Tools { get; set; } = new List<string>();

        public TechExpertiseViewModel(TechExpertise i_TechExpertise)
        {
            if (i_TechExpertise == null)
            {
                ProgrammingLanguages = new List<string>();
                Technologies = new List<string>();
                Tools = new List<string>();
                return;
            }

            ProgrammingLanguages = i_TechExpertise.ProgrammingLanguages;
            Technologies = i_TechExpertise.Technologies;
            Tools = i_TechExpertise.Tools;
        }
    }
}
