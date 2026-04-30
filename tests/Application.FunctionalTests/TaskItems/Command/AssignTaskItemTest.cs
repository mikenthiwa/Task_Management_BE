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
            AssignedId = "",
            RowVersion = await GetRowVersionAsync(taskId)
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
            AssignedId = assigneeId,
            RowVersion = await GetRowVersionAsync(taskId)
        };

        var response = await HttpClient.PostAsJsonAsync($"/api/tasks/{taskId}/assign", command);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var task = await dbContext.Tasks.FindAsync(taskId);
        task!.AssigneeId.Should().Be(assigneeId);
    }

    [Fact]
    public async Task ShouldRequireRowVersion()
    {
        var taskId = await CreateTaskAsync();
        var command = new AssignTaskCommand
        {
            AssignedId = "assignee-user-id"
        };

        var response = await HttpClient.PostAsJsonAsync($"/api/tasks/{taskId}/assign", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldReturnConflictForStaleRowVersion()
    {
        var taskId = await CreateTaskAsync();
        var staleRowVersion = await GetRowVersionAsync(taskId);
        const string firstAssigneeId = "first-assignee-user-id";
        const string secondAssigneeId = "second-assignee-user-id";
        await EnsureUserAsync(
            firstAssigneeId,
            "first-assignee-user",
            "first-assignee-user@example.com",
            "https://example.com/first-assignee-user.png");
        await EnsureUserAsync(
            secondAssigneeId,
            "second-assignee-user",
            "second-assignee-user@example.com",
            "https://example.com/second-assignee-user.png");

        var firstCommand = new AssignTaskCommand
        {
            AssignedId = firstAssigneeId,
            RowVersion = staleRowVersion
        };
        var secondCommand = new AssignTaskCommand
        {
            AssignedId = secondAssigneeId,
            RowVersion = staleRowVersion
        };

        var firstResponse = await HttpClient.PostAsJsonAsync($"/api/tasks/{taskId}/assign", firstCommand);
        var secondResponse = await HttpClient.PostAsJsonAsync($"/api/tasks/{taskId}/assign", secondCommand);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var task = await dbContext.Tasks.FindAsync(taskId);
        task!.AssigneeId.Should().Be(firstAssigneeId);
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
            AssignedId = assigneeId,
            RowVersion = 1
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

    private async Task<uint> GetRowVersionAsync(Guid taskId)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var task = await dbContext.Tasks.FindAsync(taskId);
        return task!.RowVersion;
    }
}
