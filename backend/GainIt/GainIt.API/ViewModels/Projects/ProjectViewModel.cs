namespace GainIt.API.ViewModels.Projects
{
    public class ProjectViewModel
    {
        public ProjectViewModel()
        {
            
        }
        public string projectName { get; set; }

        public string projectDescription { get; set; }

        public string projectStatus { get; set; } // "Pending" , "In Progress", "Completed"

        public string difficultyLevel { get; set; }

        public string projectSource { get; set; } // If the project is from a NonprofitOrganization or a built-in project

        public DateTime createdAtUtc { get; set; } = DateTime.UtcNow;

        public List<string> teamMemberFullNames { get; set; } = new(); // names of users (Gainers)

        public string? RepositoryLink { get; set; } 
        
        public string? AssignedMentorName { get; set; }
        
        public string? OwningOrganizationName { get; set; }
        

    }
}
