using Application.Common.Interfaces;
using AutoMapper.QueryableExtensions;
using Domain.Entities;
using MediatR;

namespace Application.Features.Users.Queries;

public class GetUsersQuery: IRequest<List<UserDto>> {}

public class GetUsersHandler(IApplicationDbContext context, IMapper mapper):IRequestHandler<GetUsersQuery, List<UserDto>>
{
    public async Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        return await context.DomainUsers
            .OrderBy(u => u.Id)
            .ProjectTo<UserDto>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
