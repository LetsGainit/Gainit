using System.ComponentModel.DataAnnotations;

namespace GainIt.API.Models.Users.Expertise
{
    public class TechExpertise : UserExpertise
    {
        public List<string> ProgrammingLanguages { get; set; } = new();

        public List<string> Technologies { get; set; } = new();

        public List<string> Tools { get; set; } = new();
    }
}
