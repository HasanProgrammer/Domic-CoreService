
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Domic.Core.WebAPI.Signals;

[Authorize(Roles = "Admin,SuperAdmin")]
public class JobsSignal : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "JobsSignalGroup");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "JobsSignalGroup");
        await base.OnDisconnectedAsync(exception);
    }
}

public class JobsSignalDto {
    public string Service { get; set; }
    public string Title { get; set; }
    public string StartDate { get; set; }
    public string EndDate { get; set; }
    public string Exception { get; set; }
}