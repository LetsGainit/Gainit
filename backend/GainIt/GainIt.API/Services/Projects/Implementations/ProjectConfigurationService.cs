using System.Text.Json;
using GainIt.API.Services.Projects.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace GainIt.API.Services.Projects.Implementations
{
    public class ProjectConfigurationService : IProjectConfigurationService
    {
        private readonly ILogger<ProjectConfigurationService> _logger;
        private readonly string _templateProjectsPath;
        private readonly string _nonprofitSuggestionsPath;

        public ProjectConfigurationService(ILogger<ProjectConfigurationService> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _templateProjectsPath = Path.Combine(environment.ContentRootPath, "Data", "Projects", "template-projects.json");
            _nonprofitSuggestionsPath = Path.Combine(environment.ContentRootPath, "Data", "Projects", "nonprofit-suggestions.json");
        }

        public List<TemplateProjectDto> LoadTemplateProjects()
        {
            try
            {
                if (!File.Exists(_templateProjectsPath))
                {
                    _logger.LogWarning("Template projects file not found at: {Path}", _templateProjectsPath);
                    return new List<TemplateProjectDto>();
                }

                var jsonContent = File.ReadAllText(_templateProjectsPath);
                var templateProjects = JsonSerializer.Deserialize<List<TemplateProjectDto>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Successfully loaded {Count} template projects from configuration", templateProjects?.Count ?? 0);
                return templateProjects ?? new List<TemplateProjectDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading template projects from configuration");
                return new List<TemplateProjectDto>();
            }
        }

        public List<NonprofitProjectSuggestion> LoadNonprofitProjectSuggestions()
        {
            try
            {
                if (!File.Exists(_nonprofitSuggestionsPath))
                {
                    _logger.LogWarning("Nonprofit suggestions file not found at: {Path}", _nonprofitSuggestionsPath);
                    return new List<NonprofitProjectSuggestion>();
                }

                var jsonContent = File.ReadAllText(_nonprofitSuggestionsPath);
                var nonprofitSuggestions = JsonSerializer.Deserialize<List<NonprofitProjectSuggestion>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("Successfully loaded {Count} nonprofit project suggestions from configuration", nonprofitSuggestions?.Count ?? 0);
                return nonprofitSuggestions ?? new List<NonprofitProjectSuggestion>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading nonprofit project suggestions from configuration");
                return new List<NonprofitProjectSuggestion>();
            }
        }

        public async Task<ProjectConfigurationValidationResult> ValidateConfigurationAsync()
        {
            var result = new ProjectConfigurationValidationResult();

            try
            {
                // Validate template projects
                var templateProjects = LoadTemplateProjects();
                result.TotalTemplateProjects = templateProjects.Count;

                if (templateProjects.Any())
                {
                    foreach (var project in templateProjects)
                    {
                        if (string.IsNullOrWhiteSpace(project.ProjectName))
                            result.Errors.Add($"Template project {project.ProjectId} has empty project name");
                        
                        if (string.IsNullOrWhiteSpace(project.ProjectDescription))
                            result.Errors.Add($"Template project {project.ProjectId} has empty description");
                        
                        if (project.DurationDays <= 0)
                            result.Errors.Add($"Template project {project.ProjectId} has invalid duration: {project.DurationDays} days");
                    }
                }
                else
                {
                    result.Warnings.Add("No template projects found in configuration");
                }

                // Validate nonprofit suggestions
                var nonprofitSuggestions = LoadNonprofitProjectSuggestions();
                result.TotalNonprofitSuggestions = nonprofitSuggestions.Count;

                if (nonprofitSuggestions.Any())
                {
                    foreach (var suggestion in nonprofitSuggestions)
                    {
                        if (string.IsNullOrWhiteSpace(suggestion.ProjectName))
                            result.Errors.Add($"Nonprofit suggestion {suggestion.ProjectId} has empty project name");
                        
                        if (string.IsNullOrWhiteSpace(suggestion.NonprofitName))
                            result.Errors.Add($"Nonprofit suggestion {suggestion.ProjectId} has empty nonprofit name");
                    }
                }
                else
                {
                    result.Warnings.Add("No nonprofit project suggestions found in configuration");
                }

                result.IsValid = !result.Errors.Any();
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Configuration validation failed: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        public DateTime GetConfigurationLastModified()
        {
            try
            {
                var templateModified = File.Exists(_templateProjectsPath) ? File.GetLastWriteTime(_templateProjectsPath) : DateTime.MinValue;
                var nonprofitModified = File.Exists(_nonprofitSuggestionsPath) ? File.GetLastWriteTime(_nonprofitSuggestionsPath) : DateTime.MinValue;
                
                return templateModified > nonprofitModified ? templateModified : nonprofitModified;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
    }
}
