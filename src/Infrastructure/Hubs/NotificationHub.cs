using Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Hubs;

public class NotificationHub : Hub
{
    public async Task SendMessage(string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", message);
    }
}
