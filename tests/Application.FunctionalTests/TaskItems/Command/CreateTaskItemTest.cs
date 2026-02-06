using System.Net;
using System.Net.Http.Json;
using Application.Features.Tasks.Command.CreateTask;
using FluentAssertions;

namespace Application.FunctionalTests.TaskItems.Command;

public class CreateTaskItemTest(CustomWebApplicationFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task ShouldRequireMinimumFields()
    {
        var command = new CreateTaskCommand
        {
            Title = "",
            Description = ""
        };

        var response = await HttpClient.PostAsJsonAsync("/api/tasks", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task ShouldCreateTask()
    {
        var command = new CreateTaskCommand
        {
            Title = "Test Task",
            Description = "This is a test task"
        };

        var response = await HttpClient.PostAsJsonAsync("/api/tasks", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
