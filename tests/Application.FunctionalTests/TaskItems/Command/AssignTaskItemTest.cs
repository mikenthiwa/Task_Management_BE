using System.Net;
using System.Net.Http.Json;
using Application.Features.Tasks.Command.AssignTask;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using TaskEntity = Domain.Entities.Task;

namespace Application.FunctionalTests.TaskItems.Command;

public class AssignTaskItemTest(CustomWebApplicationFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task ShouldRequireAssignedId()
    {
        var taskId = await CreateTaskAsync();
        var command = new AssignTaskCommand
        {
            AssignedId = ""
        };

        var response = await HttpClient.PostAsJsonAsync($"/api/tasks/{taskId}/assign", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldAssignTask()
    {
        var taskId = await CreateTaskAsync();
        const string assigneeId = "assignee-user-id";
        await EnsureUserAsync(
            assigneeId,
            "assignee-user",
            "assignee-user@example.com",
            "https://example.com/assignee-user.png");

        var command = new AssignTaskCommand
        {
            AssignedId = assigneeId
        };

        var response = await HttpClient.PostAsJsonAsync($"/api/tasks/{taskId}/assign", command);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var task = await dbContext.Tasks.FindAsync(taskId);
        task!.AssigneeId.Should().Be(assigneeId);
    }

    [Fact]
    public async Task ShouldReturnNotFoundForMissingTask()
    {
        const string assigneeId = "assignee-user-id-2";
        await EnsureUserAsync(
            assigneeId,
            "assignee-user-2",
            "assignee-user-2@example.com",
            "https://example.com/assignee-user-2.png");

        var command = new AssignTaskCommand
        {
            AssignedId = assigneeId
        };

        var response = await HttpClient.PostAsJsonAsync($"/api/tasks/{Guid.NewGuid()}/assign", command);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> CreateTaskAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var task = new TaskEntity
        {
            Title = "Test Task",
            Description = "Task for assignment test",
            Status = 0,
            Priority = 0,
            CreatorId = "test-user-id"
        };
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync();
        return task.Id;
    }
}
