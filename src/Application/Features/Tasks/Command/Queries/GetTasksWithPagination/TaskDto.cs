using System.Text.Json.Serialization;
using Domain.Enum;

using Task = Domain.Entities.Task;

namespace Application.Features.Tasks.Command.Queries.GetTasksWithPagination;

public class TaskDto
{
    public int Id { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public Status Status { get; init; }
    [JsonIgnore]
    public string? AssigneeId { get; set; }
    [JsonIgnore]
    public string? CreatorId { get; set; }

    public string? AssignedTo { get; set; }
    public string? Creator { get; set; }


    public class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Task, TaskDto>()
            .ForMember(
                d => d.AssignedTo, o => o.MapFrom(s => s.Assignee != null ? s.Assignee.Username : null)
            )
            .ForMember(
                d => d.Creator,  o => o.MapFrom(s => s.Creator != null ? s.Creator.Username : null)
            );
        }
    }
}
