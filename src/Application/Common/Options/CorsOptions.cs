namespace Application.Common.Options;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "TaskManagementCors";
    public string AllowedOrigins { get; init; } = String.Empty;
}
