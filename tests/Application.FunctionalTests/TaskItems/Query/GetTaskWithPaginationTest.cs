using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.Models;
using Application.Features.Tasks.Queries.GetTasksWithPagination;
using Application.Models;
using Domain.Enum;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using TaskEntity = Domain.Entities.Task;

namespace Application.FunctionalTests.TaskItems.Query;

public class GetTaskWithPaginationTest(CustomWebApplicationFactory factory) : BaseFunctionalTest(factory)
{
    [Fact]
    public async Task ShouldReturnPaginatedTasksForAssignee()
    {
        const string assigneeId = "pagination-user-id";
        await EnsureUserAsync(
            assigneeId,
            "pagination-user",
            "pagination-user@example.com",
            "https://example.com/pagination-user.png");

        await SeedTasksAsync(assigneeId, Status.New, count: 12, titlePrefix: "Pagination");

        var response = await HttpClient.GetAsync($"/api/Tasks?AssigneeId={assigneeId}&PageNumber=2&PageSize=5");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<PaginatedList<TaskDto>>>(JsonOptions());
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PageNumber.Should().Be(2);
        result.Data.PageSize.Should().Be(5);
        result.Data.Count.Should().Be(12);
        result.Data.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task ShouldFilterByStatus()
    {
        const string assigneeId = "status-user-id";
        await EnsureUserAsync(
            assigneeId,
            "status-user",
            "status-user@example.com",
            "https://example.com/status-user.png");

        await SeedTasksAsync(assigneeId, Status.Completed, count: 3, titlePrefix: "Completed");
        await SeedTasksAsync(assigneeId, Status.New, count: 2, titlePrefix: "New");

        var response = await HttpClient.GetAsync($"/api/Tasks?AssigneeId={assigneeId}&status=Completed&PageNumber=1&PageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<PaginatedList<TaskDto>>>(JsonOptions());
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Count.Should().Be(3);
        result.Data.Items.Should().OnlyContain(item => item.Status == Status.Completed);
    }

    private async Task SeedTasksAsync(string assigneeId, Status status, int count, string titlePrefix)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTimeOffset.UtcNow;
        var tasks = Enumerable.Range(1, count).Select(index => new TaskEntity
        {
            Title = $"{titlePrefix} Task {index}",
            Description = $"{titlePrefix} task {index}",
            Status = status,
            Priority = Priority.Low,
            CreatorId = "test-user-id",
            AssigneeId = assigneeId,
            CreatedAt = now.AddMinutes(-index)
        });

        dbContext.Tasks.AddRange(tasks);
        await dbContext.SaveChangesAsync();
    }

    private static JsonSerializerOptions JsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
