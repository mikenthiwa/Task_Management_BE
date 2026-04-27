using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Infrastructure.Hubs;

public sealed class NotificationUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? connection.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    }
}
