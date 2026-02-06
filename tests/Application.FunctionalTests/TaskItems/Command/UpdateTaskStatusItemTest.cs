using System.Net;
using System.Net.Http.Json;
using Application.Features.Tasks.Command.UpdateTaskStatus;
using Domain.Enum;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using TaskEntity = Domain.Entities.Task;

namespace Application.FunctionalTests.TaskItems.Command;

public class UpdateTaskStatusItemTest(CustomWebApplicationFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task ShouldUpdateTaskStatusWhenAssignedUser()
    {
        var taskId = await CreateTaskAsync(assigneeId: "test-user-id");
        var command = new UpdateTaskStatusCommand
        {
            Status = Status.InProgress
        };

        var response = await SendPatchAsync($"/api/tasks/{taskId}/status", command);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var task = await dbContext.Tasks.FindAsync(taskId);
        task!.Status.Should().Be(Status.InProgress);
    }

    [Fact]
    public async Task ShouldRequireValidStatus()
    {
        var taskId = await CreateTaskAsync(assigneeId: "test-user-id");
        var command = new UpdateTaskStatusCommand
        {
            Status = (Status)999
        };

        var response = await SendPatchAsync($"/api/tasks/{taskId}/status", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldReturnNotFoundForMissingTask()
    {
        var command = new UpdateTaskStatusCommand
        {
            Status = Status.Completed
        };

        var response = await SendPatchAsync($"/api/tasks/{Guid.NewGuid()}/status", command);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldReturnUnauthorizedWhenNotAssignedUser()
    {
        const string assigneeId = "assignee-user-id";
        await EnsureUserAsync(
            assigneeId,
            "assignee-user",
            "assignee-user@example.com",
            "https://example.com/assignee-user.png");
        var taskId = await CreateTaskAsync(assigneeId);

        var command = new UpdateTaskStatusCommand
        {
            Status = Status.InProgress
        };

        var response = await SendPatchAsync($"/api/tasks/{taskId}/status", command);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<Guid> CreateTaskAsync(string assigneeId)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var task = new TaskEntity
        {
            Title = "Test Task",
            Description = "Task for status update test",
            Status = Status.New,
            Priority = Priority.Low,
            CreatorId = "test-user-id",
            AssigneeId = assigneeId
        };
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync();
        return task.Id;
    }

    private Task<HttpResponseMessage> SendPatchAsync<T>(string url, T body)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(body)
        };
        return HttpClient.SendAsync(request);
    }
}
