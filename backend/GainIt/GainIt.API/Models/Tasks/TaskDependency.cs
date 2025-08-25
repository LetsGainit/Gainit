using System.Text.Json.Serialization;

namespace GainIt.API.Models.Tasks
{
    public class TaskDependency
    {
        public Guid TaskId { get; set; }
        [JsonIgnore] public required ProjectTask Task { get; set; }

        public Guid DependsOnTaskId { get; set; }
        [JsonIgnore] public required ProjectTask DependsOn { get; set; }
    }
}
