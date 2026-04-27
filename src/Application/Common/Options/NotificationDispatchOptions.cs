namespace Application.Common.Options;

public sealed class NotificationDispatchOptions
{
    public const string SectionName = "Notifications";

    public string DispatchMode { get; init; } = NotificationDispatchModes.InProcess;
}

public static class NotificationDispatchModes
{
    public const string InProcess = "InProcess";
    public const string RabbitMq = "RabbitMq";
}
