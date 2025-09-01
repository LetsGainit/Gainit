using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GainIt.API.Models.Projects
{
    [Owned]
    public class RagContext
    {
        [Required, StringLength(5000)]
        public string SearchableText { get; set; } = string.Empty;

        [Required]
        public List<string> Tags { get; set; } = new();

        [Required]
        public List<string> SkillLevels { get; set; } = new();

        [Required, StringLength(100)]
        public string ProjectType { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Domain { get; set; } = string.Empty;

        [Required]
        public List<string> LearningOutcomes { get; set; } = new();

        [Required]
        public List<string> ComplexityFactors { get; set; } = new();
    }
}
