using Infrastructure.Identity;
using Task_Management_BE.Infrastructure;

namespace Task_Management_BE.Endpoints;

public class Users : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapIdentityApi<ApplicationUser>();
        
    }
}
