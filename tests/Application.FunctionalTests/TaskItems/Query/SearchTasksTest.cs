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

public class SearchTasksTest(CustomWebApplicationFactory factory) : BaseFunctionalTest(factory)
{
    // [Fact]
    // public async Task ShouldReturnMatchingTasksByTitleOrDescription()
    // {
    //     await SeedTasksAsync();
    //
    //     var response = await HttpClient.GetAsync("/api/tasks/search?q=alpha");
    //     response.StatusCode.Should().Be(HttpStatusCode.OK);
    //
    //     var result = await response.Content.ReadFromJsonAsync<Result<PaginatedList<TaskDto>>>(JsonOptions());
    //     result.Should().NotBeNull();
    //     result!.Success.Should().BeTrue();
    //     result.Data.Should().NotBeNull();
    //     result.Data!.Items.Should().NotBeEmpty();
    //     result.Data.Items.Should().OnlyContain(item =>
    //         (item.Title != null && item.Title.Contains("alpha", StringComparison.OrdinalIgnoreCase)) ||
    //         (item.Description != null && item.Description.Contains("alpha", StringComparison.OrdinalIgnoreCase)));
    // }

    [Fact]
    public async Task ShouldReturnBadRequestWhenQueryMissing()
    {
        var response = await HttpClient.GetAsync("/api/tasks/search");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task SeedTasksAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTimeOffset.UtcNow;
        var tasks = new[]
        {
            new TaskEntity
            {
                Title = "Alpha task",
                Description = "First alpha task",
                Status = Status.New,
                Priority = Priority.Low,
                CreatorId = "test-user-id",
                AssigneeId = "test-user-id",
                CreatedAt = now.AddMinutes(-2)
            },
            new TaskEntity
            {
                Title = "Beta task",
                Description = "Alpha appears in description",
                Status = Status.New,
                Priority = Priority.Low,
                CreatorId = "test-user-id",
                AssigneeId = "test-user-id",
                CreatedAt = now.AddMinutes(-1)
            },
            new TaskEntity
            {
                Title = "Gamma task",
                Description = "Unrelated",
                Status = Status.New,
                Priority = Priority.Low,
                CreatorId = "test-user-id",
                AssigneeId = "test-user-id",
                CreatedAt = now
            }
        };

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
