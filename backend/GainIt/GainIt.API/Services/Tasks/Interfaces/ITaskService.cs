using GainIt.API.Models.Enums.Tasks;
using GainIt.API.DTOs.ViewModels.Tasks;
using GainIt.API.DTOs.Requests.Tasks;

namespace GainIt.API.Services.Tasks.Interfaces
{
    public interface ITaskService
    {
        // Queries
        Task<ProjectTaskViewModel?> GetTaskAsync(Guid i_ProjectId, Guid i_TaskId, Guid i_ActorUserId);

        //Task<IReadOnlyList<ProjectTaskListItemViewModel>> ListMyTasksAsync(Guid i_ProjectId, Guid i_UserId, TaskListQueryDto i_TaskListQuery);
        //Task<IReadOnlyList<ProjectTaskListItemViewModel>> ListBoardAsync(Guid i_ProjectId, TaskBoardQueryDto i_TaskBoardQuery);

        // CRUD
        Task<ProjectTaskViewModel> CreateAsync(Guid i_ProjectId, ProjectTaskCreateDto i_TaskCreateModel, Guid i_ActorUserId);
        Task<ProjectTaskViewModel> UpdateAsync(Guid i_ProjectId, Guid i_TaskId, ProjectTaskUpdateDto i_TaskUpdateModel, Guid i_ActorUserId);
        Task DeleteAsync(Guid i_ProjectId, Guid i_TaskId, Guid i_ActorUserId);

        // Status / Ordering
        Task<ProjectTaskViewModel> ChangeStatusAsync(Guid i_ProjectId, Guid i_TaskId, eTaskStatus i_NewStatus, Guid i_ActorUserId);
        Task<ProjectTaskViewModel> ReorderAsync(Guid i_ProjectId, Guid i_TaskId, int i_NewOrderIndex, Guid i_ActorUserId);

        // Dependencies
        Task AddDependencyAsync(Guid i_ProjectId, Guid i_TaskId, Guid i_DependsOnTaskId, Guid i_ActorUserId);
        Task RemoveDependencyAsync(Guid i_ProjectId, Guid i_TaskId, Guid i_DependsOnTaskId, Guid i_ActorUserId);

        // Subtasks
        Task<IReadOnlyList<ProjectSubtaskViewModel>> ListSubtasksAsync(Guid i_ProjectId, Guid i_TaskId);
        Task<ProjectSubtaskViewModel> AddSubtaskAsync(Guid i_ProjectId, Guid i_TaskId, SubtaskCreateDto i_SubtaskCreateModel, Guid i_ActorUserId);
        Task<ProjectSubtaskViewModel> UpdateSubtaskAsync(Guid i_ProjectId, Guid i_TaskId, Guid i_SubtaskId, SubtaskUpdateDto i_SubtaskUpdateModel, Guid i_ActorUserId);
        Task RemoveSubtaskAsync(Guid i_ProjectId, Guid i_TaskId, Guid i_SubtaskId, Guid i_ActorUserId);
        Task<ProjectSubtaskViewModel> ToggleSubtaskAsync(Guid i_ProjectId, Guid i_TaskId, Guid i_SubtaskId, bool i_IsDone, Guid i_ActorUserId);

        // References
        Task<IReadOnlyList<ProjectTaskReferenceViewModel>> ListReferencesAsync(Guid i_ProjectId, Guid i_TaskId);
        Task<ProjectTaskReferenceViewModel> AddReferenceAsync(Guid i_ProjectId, Guid i_TaskId, TaskReferenceCreateDto i_ReferenceCreateModel, Guid i_ActorUserId);
        Task RemoveReferenceAsync(Guid i_ProjectId, Guid i_TaskId, Guid i_ReferenceId, Guid i_ActorUserId);
    }
}
