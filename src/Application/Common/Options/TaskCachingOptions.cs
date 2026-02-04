namespace Application.Common.Options;

public sealed class TaskCachingOptions
{
    public bool Enabled { get; set; } = true;
    public int TtlSeconds { get; set; } = 30;
}
