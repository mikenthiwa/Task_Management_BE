using Application.Common.Interfaces;
using Application.Common.Mappings;
using Application.Common.Models;
using Application.Common.Options;
using Application.Features.Tasks.Caching;
using AutoMapper.QueryableExtensions;
using Domain.Enum;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Features.Tasks.Queries.GetTasksWithPagination;

public record GetTaskWithQuery : IRequest<PaginatedList<TaskDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public Status? Status { get; init; }
    public string? AssigneeId { get; init; }
}

public class GetTasksWithPaginationHandler(
    IApplicationDbContext context,
    IMapper mapper,
    IMemoryCache cache,
    IOptions<TaskCachingOptions> cacheOptions,
    IRedisCacheService redisCache,
    ILogger<GetTasksWithPaginationHandler> logger) : IRequestHandler<GetTaskWithQuery, PaginatedList<TaskDto>>
{
    public async Task<PaginatedList<TaskDto>> Handle(GetTaskWithQuery request, CancellationToken cancellationToken)
    {
        var options = cacheOptions.Value;
        if (!options.Enabled || options.TtlSeconds <= 0)
        {
            return await FetchTasksAsync(request, cancellationToken);
        }

        var version = TaskCacheKey.GetVersion(cache);
        var cacheKey = TaskCacheKey.BuildListKey(request, version);

        if (cache.TryGetValue(cacheKey, out PaginatedList<TaskDto>? cached) && cached is not null)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Tasks cache hit for {CacheKey}", cacheKey);
            }
            return cached;
        }
        
        var cachedData = await redisCache.GetAsync<PaginatedList<TaskDto>>(cacheKey, cancellationToken);
        if(cachedData is not null)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Tasks Redis cache hit for {CacheKey}", cacheKey);
            }

            return cachedData;
        }
        
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Tasks cache miss for {CacheKey}", cacheKey);
        }
        var result = await FetchTasksAsync(request, cancellationToken);
        cache.Set(cacheKey, result, TimeSpan.FromSeconds(options.TtlSeconds));
        await redisCache.SetAsync(cacheKey, result, TimeSpan.FromSeconds(options.TtlSeconds), cancellationToken);
        return result;
    }

    private Task<PaginatedList<TaskDto>> FetchTasksAsync(GetTaskWithQuery request, CancellationToken cancellationToken)
    {
        return context.Tasks
            .AsNoTracking()
            .TaskQuery(request)
            .OrderByDescending(t => t.CreatedAt)
            .ProjectTo<TaskDto>(mapper.ConfigurationProvider)
            .PaginatedListAsync(request.PageNumber, request.PageSize);
    }
}
