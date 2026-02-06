using Application.Features.Tasks.Queries.GetTasksWithPagination;
using Microsoft.Extensions.Caching.Memory;

namespace Application.Features.Tasks.Caching;

public static class TaskCacheKey
{
    private const string VersionCacheKey = "tasks:list:version";

    public static string BuildListKey(GetTaskWithQuery request, int version)
    {
        var statusPart = request.Status?.ToString() ?? "any";
        var assigneePart = string.IsNullOrWhiteSpace(request.AssigneeId) ? "any" : request.AssigneeId.Trim();
        return $"tasks:list:v{version}:status={statusPart}:assignee={assigneePart}:page={request.PageNumber}:size={request.PageSize}";
    }

    public static int GetVersion(IMemoryCache cache)
    {
        if (cache.TryGetValue(VersionCacheKey, out int version))
        {
            return version;
        }

        version = 1;
        cache.Set(VersionCacheKey, version);
        return version;
    }

    public static void BumpVersion(IMemoryCache cache)
    {
        var version = GetVersion(cache);
        cache.Set(VersionCacheKey, version + 1);
    }
}
