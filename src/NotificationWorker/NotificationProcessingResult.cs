namespace NotificationWorker;

public sealed record NotificationProcessingResult(bool Succeeded, string? Error)
{
    public static NotificationProcessingResult Success() => new(true, null);
    public static NotificationProcessingResult Failure(string error) => new(false, error);
}
