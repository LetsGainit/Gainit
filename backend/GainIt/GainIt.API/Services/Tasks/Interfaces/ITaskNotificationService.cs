using GainIt.API.DTOs.ViewModels.Tasks;
using GainIt.API.DTOs.Requests.Tasks;

namespace GainIt.API.Services.Tasks.Interfaces
{
    public interface INotificationService
    {
        Task TaskCreatedAsync(Guid i_ProjectId, ProjectTaskViewModel i_ProjectTaskViewModel);
        Task TaskUnblockedAsync(Guid i_ProjectId, Guid i_TaskId);
        Task TaskStatusChangedAsync(Guid i_ProjectId, Guid i_TaskId, string i_OldStatus, string i_NewStatus);
    }
}
