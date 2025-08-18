using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GainIt.API.Models.Users.Expertise
{
    public class TechExpertise : UserExpertise
    {
        [JsonIgnore]
        public List<string> ProgrammingLanguages { get; set; } = new();

        [JsonIgnore]
        public List<string> Technologies { get; set; } = new();

        [JsonIgnore]
        public List<string> Tools { get; set; } = new();
    }
}
