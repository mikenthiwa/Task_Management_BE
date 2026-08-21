namespace Application.Common.Models;

public sealed record DeadLetterNotification(
    Guid MessageId,
    string Payload,
    string Error,
    DateTimeOffset FailedAt);
