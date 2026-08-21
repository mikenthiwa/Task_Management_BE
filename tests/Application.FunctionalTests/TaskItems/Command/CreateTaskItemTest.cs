using System.Net;
using System.Net.Http.Json;
using Application.Features.Tasks.Command.CreateTask;
using Domain.Events;
using FluentAssertions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskEntity = Domain.Entities.Task;

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

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var outboxMessage = await dbContext.OutboxMessages.SingleAsync();
        outboxMessage.RoutingKey.Should().Be("task.created");
        outboxMessage.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task ShouldRollbackTaskAndOutboxMessageWhenSaveFails()
    {
        var marker = Guid.NewGuid().ToString();
        var task = new TaskEntity
        {
            Title = $"Task that cannot be persisted {marker}",
            Description = "The creator does not exist",
            CreatorId = $"missing-user-{Guid.NewGuid()}"
        };
        task.AddDomainEvent(new TaskCreatedEvent(task));

        using (var scope = Factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Tasks.Add(task);

            var save = () => dbContext.SaveChangesAsync();
            await save.Should().ThrowAsync<DbUpdateException>();
        }

        using var verificationScope = Factory.Services.CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await verificationContext.Tasks.AnyAsync(entity => entity.Id == task.Id)).Should().BeFalse();
        var outboxPayloads = await verificationContext.OutboxMessages
            .Select(message => message.Payload)
            .ToListAsync();
        outboxPayloads.Should().NotContain(payload => payload.Contains(marker));
    }
}
