using Azure.AI.OpenAI;
using GainIt.API.Data;
using GainIt.API.DTOs.Requests.Tasks;
using GainIt.API.DTOs.ViewModels.Tasks;
using GainIt.API.Models.Enums.Tasks;
using GainIt.API.Models.Projects;
using GainIt.API.Models.Tasks;
using GainIt.API.Models.Tasks.AIPlanning;
using GainIt.API.Options;
using GainIt.API.Services.Tasks.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Text.Json;


namespace GainIt.API.Services.Tasks.Implementations
{
    public class PlanningService : IPlanningService
    {
        private readonly AzureOpenAIClient r_azureOpenAIClient;
        private readonly ChatClient r_chatClient;
        private readonly GainItDbContext r_DbContext;
        private readonly ILogger<PlanningService> r_logger;

        public PlanningService(
            AzureOpenAIClient i_AzureOpenAIClient,
            IOptions<OpenAIOptions> i_OpenAIOptionsAccessor,
            GainItDbContext i_DbContext,
            ILogger<PlanningService> i_logger)
        {
            r_azureOpenAIClient = i_AzureOpenAIClient;
            r_chatClient = i_AzureOpenAIClient.GetChatClient(i_OpenAIOptionsAccessor.Value.ChatDeploymentName);
            r_DbContext = i_DbContext;
            r_logger = i_logger;
        }

        public async Task<PlanApplyResultViewModel> GenerateForProjectAsync(
            Guid i_ProjectId,
            PlanRequestDto i_PlanRequest,
            Guid i_ActorUserId)
        {
            r_logger.LogInformation("Generating AI roadmap for project: ProjectId={ProjectId}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_ActorUserId);

            try
            {
                var project = await r_DbContext.Projects
                    .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.User)
                    .FirstOrDefaultAsync(p => p.ProjectId == i_ProjectId)
                    ?? throw new KeyNotFoundException("Project not found.");

                r_logger.LogInformation("Project found: ProjectId={ProjectId}, ProjectName={ProjectName}, MemberCount={MemberCount}", 
                    i_ProjectId, project.ProjectName, project.ProjectMembers.Count);

                var result = await generateAiPlanAsync(project, i_PlanRequest);

                r_logger.LogInformation("AI roadmap generated successfully: ProjectId={ProjectId}, Milestones={MilestoneCount}, Tasks={TaskCount}", 
                    i_ProjectId, result.CreatedMilestones.Count, result.CreatedTasks.Count);

                return result;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error generating AI roadmap: ProjectId={ProjectId}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_ActorUserId);
                throw;
            }
        }

        public async Task<TaskElaborationResultViewModel> ElaborateTaskAsync(
            Guid i_ProjectId,
            Guid i_TaskId,
            TaskElaborationRequestDto i_ElaborationRequest,
            Guid i_ActorUserId)
        {
            r_logger.LogInformation("Elaborating task: ProjectId={ProjectId}, TaskId={TaskId}, ActorUserId={ActorUserId}", 
                i_ProjectId, i_TaskId, i_ActorUserId);

            try
            {
                var task = await r_DbContext.ProjectTasks
                    .Include(t => t.Project)
                    .Include(t => t.Milestone)
                    .FirstOrDefaultAsync(t => t.ProjectId == i_ProjectId && t.TaskId == i_TaskId)
                    ?? throw new KeyNotFoundException("Task not found.");

                r_logger.LogInformation("Task found: ProjectId={ProjectId}, TaskId={TaskId}, Title={Title}, Type={Type}", 
                    i_ProjectId, i_TaskId, task.Title, task.Type);

                var elaboration = await generateTaskElaborationAsync(task, i_ElaborationRequest);

                var result = new TaskElaborationResultViewModel
                {
                    ProjectId = i_ProjectId,
                    TaskId = i_TaskId,
                    Notes = elaboration
                };

                r_logger.LogInformation("Task elaboration completed: ProjectId={ProjectId}, TaskId={TaskId}, NotesCount={NotesCount}", 
                    i_ProjectId, i_TaskId, result.Notes.Count);

                return result;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error elaborating task: ProjectId={ProjectId}, TaskId={TaskId}, ActorUserId={ActorUserId}", 
                    i_ProjectId, i_TaskId, i_ActorUserId);
                throw;
            }
        }

        private async Task<PlanApplyResultViewModel> generateAiPlanAsync(UserProject project, PlanRequestDto planRequest)
        {
            r_logger.LogInformation("Generating AI plan: ProjectId={ProjectId}, ProjectName={ProjectName}", 
                project.ProjectId, project.ProjectName);

            try
            {
                var projectContext = buildProjectContext(project, planRequest);
                r_logger.LogInformation("Project context built: ProjectId={ProjectId}, ContextLength={ContextLength}", 
                    project.ProjectId, projectContext.Length);

                var roadmapJson = await generateRoadmapWithAiAsync(projectContext);
                r_logger.LogInformation("AI roadmap generated: ProjectId={ProjectId}, JsonLength={JsonLength}", 
                    project.ProjectId, roadmapJson.Length);

                var roadmap = parseRoadmapJson(roadmapJson);
                r_logger.LogInformation("Roadmap parsed: ProjectId={ProjectId}, Milestones={MilestoneCount}, Tasks={TaskCount}", 
                    project.ProjectId, roadmap.Milestones.Count, roadmap.Tasks.Count);

                var result = await applyRoadmapToDatabaseAsync(project, roadmap);
                r_logger.LogInformation("Roadmap applied to database: ProjectId={ProjectId}, CreatedMilestones={CreatedMilestones}, CreatedTasks={CreatedTasks}", 
                    project.ProjectId, result.CreatedMilestones.Count, result.CreatedTasks.Count);

                return result;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error generating AI plan: ProjectId={ProjectId}", project.ProjectId);
                throw;
            }
        }

        private string buildProjectContext(UserProject project, PlanRequestDto planRequest)
        {
            r_logger.LogInformation("Building project context: ProjectId={ProjectId}", project.ProjectId);

            // Get actual team members and their roles
            var teamMembers = project.ProjectMembers
                .Where(pm => pm.LeftAtUtc == null)
                .Select(pm => $"{pm.UserRole}: {pm.User.FullName}")
                .ToList();

            var context = $@"
Project Information:
- Name: {project.ProjectName}
- Description: {project.ProjectDescription}
- Goals: [{string.Join(", ", project.Goals ?? new List<string>())}]
- Technologies: [{string.Join(", ", project.Technologies ?? new List<string>())}]
- Difficulty: {project.DifficultyLevel}
- Duration: {project.Duration.TotalDays} days
- Source: {project.ProjectSource}
- Status: {project.ProjectStatus}

Team Information:
- Team Members: [{string.Join(", ", teamMembers)}]
- Member Count: {teamMembers.Count}
- Team Roles: [{string.Join(", ", teamMembers.Select(tm => tm.Split(':')[0].Trim()).Distinct())}]

Planning Requirements:
- Goal: {planRequest.Goal ?? "Not specified"}
- Constraints: {planRequest.Constraints ?? "None"}
- Preferred Technologies: {planRequest.PreferredTechnologies ?? "Use project technologies"}
- Start Date: {planRequest.StartDateUtc?.ToString("yyyy-MM-dd") ?? "Not specified"}
- Target Due Date: {planRequest.TargetDueDateUtc?.ToString("yyyy-MM-dd") ?? "Not specified"}

Please create a comprehensive roadmap with milestones and tasks that:
1. Breaks down the project into logical phases
2. Assigns appropriate tasks to each actual team member role
3. Includes realistic timeframes and dependencies
4. Covers all aspects of the project (planning, development, testing, deployment)
5. Considers the project's difficulty level and actual team composition
6. Includes subtasks for complex tasks
7. Uses appropriate task types (Feature, Research, Infra, Docs, Refactor)
8. Sets realistic priorities based on dependencies and importance
9. Distributes work evenly among available team members";

            r_logger.LogInformation("Project context built successfully: ProjectId={ProjectId}, ContextLength={ContextLength}", 
                project.ProjectId, context.Length);
            return context;
        }

        private async Task<string> generateRoadmapWithAiAsync(string projectContext)
        {
            r_logger.LogInformation("Generating roadmap with AI: ContextLength={ContextLength}", projectContext.Length);

            try
            {
                var messages = new ChatMessage[]
                {
                    new SystemChatMessage(
                        "You are an expert project planner that creates comprehensive roadmaps for software projects. " +
                        "Return ONLY a valid JSON object with this exact structure:\n" +
                        "{\n" +
                        "  \"milestones\": [\n" +
                        "    {\n" +
                        "      \"title\": \"string\",\n" +
                        "      \"description\": \"string\",\n" +
                        "      \"orderIndex\": number,\n" +
                        "      \"targetDateUtc\": \"YYYY-MM-DD\"\n" +
                        "    }\n" +
                        "  ],\n" +
                        "  \"tasks\": [\n" +
                        "    {\n" +
                        "      \"title\": \"string\",\n" +
                        "      \"description\": \"string\",\n" +
                        "      \"type\": \"Feature|Research|Infra|Docs|Refactor\",\n" +
                        "      \"priority\": \"Low|Medium|High|Critical\",\n" +
                        "      \"milestoneId\": number (index of milestone in array),\n" +
                        "      \"assignedRole\": \"string\",\n" +
                        "      \"orderIndex\": number,\n" +
                        "      \"dueAtUtc\": \"YYYY-MM-DD\",\n" +
                        "      \"subtasks\": [\n" +
                        "        {\n" +
                        "          \"title\": \"string\",\n" +
                        "          \"description\": \"string\",\n" +
                        "          \"orderIndex\": number\n" +
                        "        }\n" +
                        "      ]\n" +
                        "    }\n" +
                        "  ]\n" +
                        "}\n" +
                        "Rules:\n" +
                        "1. Create 3-6 milestones that represent project phases\n" +
                        "2. Create 10-25 tasks distributed across milestones\n" +
                        "3. Include subtasks for complex tasks (2-5 subtasks per task)\n" +
                        "4. Use realistic dates and timeframes\n" +
                        "5. Assign tasks to actual team member roles appropriately\n" +
                        "6. Ensure task dependencies make sense\n" +
                        "7. Distribute work evenly among available team members\n" +
                        "8. Return ONLY the JSON, no other text"
                    ),
                    new UserChatMessage(projectContext)
                };

                var options = new ChatCompletionOptions
                {
                    Temperature = 0.3f
                };

                ChatCompletion completion = await r_chatClient.CompleteChatAsync(messages, options);
                var roadmapJson = completion.Content[0].Text.Trim();

                r_logger.LogInformation("AI roadmap generated successfully: JsonLength={JsonLength}", roadmapJson.Length);
                return roadmapJson;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error generating roadmap with AI: ContextLength={ContextLength}", projectContext.Length);
                throw;
            }
        }

        private RoadmapData parseRoadmapJson(string roadmapJson)
        {
            r_logger.LogInformation("Parsing roadmap JSON: JsonLength={JsonLength}", roadmapJson.Length);

            try
            {
                // Remove markdown code blocks if present
                if (roadmapJson.StartsWith("```"))
                {
                    int firstNewline = roadmapJson.IndexOf('\n');
                    if (firstNewline >= 0)
                    {
                        roadmapJson = roadmapJson.Substring(firstNewline + 1);
                    }
                    int lastCodeBlock = roadmapJson.LastIndexOf("```");
                    if (lastCodeBlock >= 0)
                    {
                        roadmapJson = roadmapJson.Substring(0, lastCodeBlock).Trim();
                    }
                }

                var roadmap = JsonSerializer.Deserialize<RoadmapData>(roadmapJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("Failed to deserialize roadmap JSON");

                r_logger.LogInformation("Roadmap JSON parsed successfully: Milestones={MilestoneCount}, Tasks={TaskCount}", 
                    roadmap.Milestones.Count, roadmap.Tasks.Count);
                return roadmap;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error parsing roadmap JSON: JsonLength={JsonLength}", roadmapJson.Length);
                throw new InvalidOperationException("Failed to parse AI-generated roadmap", ex);
            }
        }

        private async Task<PlanApplyResultViewModel> applyRoadmapToDatabaseAsync(UserProject project, RoadmapData roadmap)
        {
            r_logger.LogInformation("Applying roadmap to database: ProjectId={ProjectId}, Milestones={MilestoneCount}, Tasks={TaskCount}", 
                project.ProjectId, roadmap.Milestones.Count, roadmap.Tasks.Count);

            try
            {
                var result = new PlanApplyResultViewModel
                {
                    ProjectId = project.ProjectId
                };

                // Create milestones
                var createdMilestones = new List<ProjectMilestone>();
                foreach (var milestoneData in roadmap.Milestones.OrderBy(m => m.OrderIndex))
                {
                    var milestone = new ProjectMilestone
                    {
                        ProjectId = project.ProjectId,
                        Project = project,
                        Title = milestoneData.Title,
                        Description = milestoneData.Description,
                        OrderIndex = milestoneData.OrderIndex,
                        TargetDateUtc = DateTime.Parse(milestoneData.TargetDateUtc),
                        Status = eMilestoneStatus.Planned,
                        CreatedByUserId = project.ProjectMembers.FirstOrDefault()?.UserId ?? project.OwningOrganizationUserId ?? Guid.Empty
                    };

                    r_DbContext.ProjectMilestones.Add(milestone);
                    createdMilestones.Add(milestone);
                }

                await r_DbContext.SaveChangesAsync();
                r_logger.LogInformation("Milestones created: ProjectId={ProjectId}, Count={Count}", project.ProjectId, createdMilestones.Count);

                // Create tasks
                var createdTasks = new List<ProjectTask>();
                foreach (var taskData in roadmap.Tasks.OrderBy(t => t.OrderIndex))
                {
                    var milestone = createdMilestones.ElementAtOrDefault(taskData.MilestoneId);
                    if (milestone == null)
                    {
                        r_logger.LogWarning("Milestone not found for task: ProjectId={ProjectId}, TaskMilestoneId={TaskMilestoneId}, TaskTitle={TaskTitle}", 
                            project.ProjectId, taskData.MilestoneId, taskData.Title);
                        continue;
                    }

                    var task = new ProjectTask
                    {
                        ProjectId = project.ProjectId,
                        Project = project,
                        Title = taskData.Title,
                        Description = taskData.Description,
                        Type = Enum.Parse<eTaskType>(taskData.Type),
                        Priority = Enum.Parse<eTaskPriority>(taskData.Priority),
                        MilestoneId = milestone.MilestoneId,
                        Milestone = milestone,
                        AssignedRole = taskData.AssignedRole,
                        OrderIndex = taskData.OrderIndex,
                        DueAtUtc = DateTime.Parse(taskData.DueAtUtc),
                        Status = eTaskStatus.Todo,
                        CreatedByUserId = project.ProjectMembers.FirstOrDefault()?.UserId ?? project.OwningOrganizationUserId ?? Guid.Empty
                    };

                    r_DbContext.ProjectTasks.Add(task);
                    createdTasks.Add(task);

                    // Create subtasks
                    if (taskData.Subtasks != null && taskData.Subtasks.Any())
                    {
                        foreach (var subtaskData in taskData.Subtasks.OrderBy(s => s.OrderIndex))
                        {
                            var subtask = new ProjectSubtask
                            {
                                TaskId = task.TaskId,
                                Task = task,
                                Title = subtaskData.Title,
                                Description = subtaskData.Description,
                                OrderIndex = subtaskData.OrderIndex,
                                IsDone = false,
                                CreatedByUserId = project.ProjectMembers.FirstOrDefault()?.UserId ?? project.OwningOrganizationUserId ?? Guid.Empty
                            };

                            r_DbContext.ProjectSubtasks.Add(subtask);
                        }
                    }
                }

                await r_DbContext.SaveChangesAsync();
                r_logger.LogInformation("Tasks and subtasks created: ProjectId={ProjectId}, TaskCount={TaskCount}", project.ProjectId, createdTasks.Count);

                // Map to view models
                result.CreatedMilestones = createdMilestones.Select(m => new ProjectMilestoneViewModel
                {
                    MilestoneId = m.MilestoneId,
                    Title = m.Title,
                    Description = m.Description,
                    OrderIndex = m.OrderIndex,
                    TargetDateUtc = m.TargetDateUtc,
                    Status = m.Status
                }).ToList();

                result.CreatedTasks = createdTasks.Select(t => new ProjectTaskViewModel
                {
                    TaskId = t.TaskId,
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status,
                    Priority = t.Priority,
                    Type = t.Type,
                    IsBlocked = t.IsBlocked,
                    OrderIndex = t.OrderIndex,
                    CreatedAtUtc = t.CreatedAtUtc,
                    DueAtUtc = t.DueAtUtc,
                    AssignedRole = t.AssignedRole,
                    AssignedUserId = t.AssignedUserId,
                    MilestoneId = t.MilestoneId,
                    MilestoneTitle = t.Milestone?.Title,
                    Subtasks = t.Subtasks?.Select(s => new ProjectSubtaskViewModel
                    {
                        SubtaskId = s.SubtaskId,
                        Title = s.Title,
                        Description = s.Description,
                        IsDone = s.IsDone,
                        OrderIndex = s.OrderIndex,
                        CompletedAtUtc = s.CompletedAtUtc
                    }).ToList() ?? new List<ProjectSubtaskViewModel>(),
                    References = new List<ProjectTaskReferenceViewModel>(),
                    Dependencies = new List<TaskDependencyViewModel>()
                }).ToList();

                result.Notes.Add($"Generated {result.CreatedMilestones.Count} milestones and {result.CreatedTasks.Count} tasks using AI planning.");
                result.Notes.Add("Review and adjust the plan as needed for your specific project requirements.");

                r_logger.LogInformation("Roadmap applied successfully: ProjectId={ProjectId}, Milestones={MilestoneCount}, Tasks={TaskCount}", 
                    project.ProjectId, result.CreatedMilestones.Count, result.CreatedTasks.Count);

                return result;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error applying roadmap to database: ProjectId={ProjectId}", project.ProjectId);
                throw;
            }
        }

        private async Task<List<string>> generateTaskElaborationAsync(ProjectTask task, TaskElaborationRequestDto elaborationRequest)
        {
            r_logger.LogInformation("Generating task elaboration: TaskId={TaskId}, Title={Title}, Type={Type}", 
                task.TaskId, task.Title, task.Type);

            try
            {
                var taskContext = $@"
Task Information:
- Title: {task.Title}
- Description: {task.Description ?? "No description provided"}
- Type: {task.Type}
- Priority: {task.Priority}
- Status: {task.Status}
- Assigned Role: {task.AssignedRole ?? "Not assigned"}
- Due Date: {task.DueAtUtc?.ToString("yyyy-MM-dd") ?? "Not set"}

Project Context:
- Project Name: {task.Project.ProjectName}
- Project Description: {task.Project.ProjectDescription}
- Technologies: [{string.Join(", ", task.Project.Technologies ?? new List<string>())}]
- Goals: [{string.Join(", ", task.Project.Goals ?? new List<string>())}]

User Request: {elaborationRequest.AdditionalContext ?? "Provide detailed guidance for this task"}

Please provide detailed guidance including:
1. Step-by-step instructions
2. Technical considerations
3. Best practices
4. Potential challenges and solutions
5. Resources and references
6. Success criteria
7. Time estimates
8. Dependencies to consider";

                var messages = new ChatMessage[]
                {
                    new SystemChatMessage(
                        "You are an expert software development mentor that provides detailed, actionable guidance for development tasks. " +
                        "Provide clear, step-by-step instructions with technical details, best practices, and practical advice. " +
                        "Format your response as a list of detailed notes that can help the developer understand exactly what needs to be done."
                    ),
                    new UserChatMessage(taskContext)
                };

                var options = new ChatCompletionOptions
                {
                    Temperature = 0.4f
                };

                ChatCompletion completion = await r_chatClient.CompleteChatAsync(messages, options);
                var elaboration = completion.Content[0].Text.Trim();

                var notes = new List<string> { elaboration };

                r_logger.LogInformation("Task elaboration generated successfully: TaskId={TaskId}, NotesCount={NotesCount}", 
                    task.TaskId, notes.Count);

                return notes;
            }
            catch (Exception ex)
            {
                r_logger.LogError(ex, "Error generating task elaboration: TaskId={TaskId}", task.TaskId);
                return new List<string> { "Unable to generate elaboration at this time. Please try again later." };
            }
        }
    }
}
