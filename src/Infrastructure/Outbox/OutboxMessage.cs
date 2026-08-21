namespace Infrastructure.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public OutboxMessage(
        string type,
        string payload,
        string exchange,
        string routingKey,
        DateTimeOffset occurredAt)
    {
        Id = Guid.NewGuid();
        Type = type;
        Payload = payload;
        Exchange = exchange;
        RoutingKey = routingKey;
        OccurredAt = occurredAt;
        NextAttemptAt = occurredAt;
    }

    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public string Exchange { get; private set; } = string.Empty;
    public string RoutingKey { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public string? LastError { get; private set; }
    public Guid? LeaseId { get; private set; }
    public DateTimeOffset? LeasedUntil { get; private set; }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessedAt = processedAt;
        NextAttemptAt = null;
        LastError = null;
        ReleaseLease();
    }

    public void MarkPermanentlyFailed(DateTimeOffset failedAt, string error)
    {
        FailedAt = failedAt;
        LastError = error;
        NextAttemptAt = null;
        ReleaseLease();
    }

    public void ScheduleRetry(DateTimeOffset nextAttemptAt, string error)
    {
        Attempts++;
        NextAttemptAt = nextAttemptAt;
        LastError = error;
        ReleaseLease();
    }

    public void AcquireLease(Guid leaseId, DateTimeOffset leasedUntil)
    {
        LeaseId = leaseId;
        LeasedUntil = leasedUntil;
    }

    public void ReleaseLease()
    {
        LeaseId = null;
        LeasedUntil = null;
    }
}
