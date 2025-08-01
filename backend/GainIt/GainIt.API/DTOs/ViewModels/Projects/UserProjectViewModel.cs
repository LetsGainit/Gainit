﻿using GainIt.API.DTOs.ViewModels.Users;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Users;

namespace GainIt.API.DTOs.ViewModels.Projects
{
    public class UserProjectViewModel
    {
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectDescription { get; set; }
        public string ProjectStatus { get; set; }
        public string DifficultyLevel { get; set; }
        public string ProjectSource { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public List<ConciseUserViewModel> ProjectTeamMembers { get; set; } = new List<ConciseUserViewModel>();
        public string? RepositoryLink { get; set; }
        public FullNonprofitViewModel? OwningOrganization { get; set; }
        public FullMentorViewModel? AssignedMentor { get; set; }
        public string? ProjectPictureUrl { get; set; }
        public TimeSpan? Duration { get; set; }
        public List<string> OpenRoles { get; set; } = new List<string>();
        public List<string> ProgrammingLanguages { get; set; } = new List<string>();
        public List<string> Goals { get; set; } = new List<string>();
        public List<string> Technologies { get; set; } = new List<string>();

        public UserProjectViewModel(UserProject i_Project)
        {
            ProjectId = i_Project.ProjectId.ToString();
            ProjectName = i_Project.ProjectName;
            ProjectDescription = i_Project.ProjectDescription;
            ProjectStatus = i_Project.ProjectStatus.ToString();
            DifficultyLevel = i_Project.DifficultyLevel.ToString();
            ProjectSource = i_Project.ProjectSource.ToString();
            CreatedAtUtc = i_Project.CreatedAtUtc;
            RepositoryLink = i_Project.RepositoryLink;

            ProjectTeamMembers = i_Project.ProjectMembers
                .Select(member => new ConciseUserViewModel(member))
                .ToList();

            OwningOrganization = i_Project.OwningOrganization != null
                ? new FullNonprofitViewModel(i_Project.OwningOrganization, new List<UserProject>())
                : null;

            ProjectPictureUrl = i_Project.ProjectPictureUrl;
            Duration = i_Project.Duration;
            OpenRoles = i_Project.RequiredRoles;
            ProgrammingLanguages = i_Project.ProgrammingLanguages;
            Goals = i_Project.Goals;
            Technologies = i_Project.Technologies;
        }
    }
}
