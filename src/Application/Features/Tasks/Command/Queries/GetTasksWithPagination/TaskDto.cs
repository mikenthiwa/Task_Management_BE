using Domain.Enum;

using Task = Domain.Entities.Task;

namespace Application.Features.Tasks.Command.Queries.GetTasksWithPagination;

public class TaskDto
{
    public int Id { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public Status Status { get; init; }
    public string? AssignedTo { get; init; }

    public class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Task, TaskDto>();
        }
    }
}
