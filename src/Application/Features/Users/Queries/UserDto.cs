using Domain.Entities;

namespace Application.Features.Users.Queries;

public class UserDto
{
    public string? Id { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
    
    public class Mapping: Profile
    {
        public Mapping()
        {
            CreateMap<DomainUser, UserDto>();
        }
    }
}
