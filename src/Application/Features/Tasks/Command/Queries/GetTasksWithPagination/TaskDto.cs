using System.Text.Json.Serialization;
using Domain.Entities;
using Domain.Enum;

using Task = Domain.Entities.Task;

namespace Application.Features.Tasks.Command.Queries.GetTasksWithPagination;

public class TaskDto
{
    public int Id { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public Status Status { get; init; }
    public UserBriefDto? Assignee { get; init; }
    public UserBriefDto? Creator { get; init; }
    public record UserBriefDto(string Id, string Username, string Email);


    public class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<DomainUser, UserBriefDto>();
            CreateMap<Task, TaskDto>()
                .ForMember(dto => dto.Assignee, o => o.MapFrom(task => task.Assignee))
                .ForMember(dto => dto.Creator, o => o.MapFrom(task => task.Creator));
        }
    }
}
