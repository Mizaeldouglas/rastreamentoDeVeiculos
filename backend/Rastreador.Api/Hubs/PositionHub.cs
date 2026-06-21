using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Rastreador.Api.Extensions;

namespace Rastreador.Api.Hubs;

[Authorize]
public class PositionHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var companyId = Context.User?.GetCompanyId() ?? 0;
        await Groups.AddToGroupAsync(Context.ConnectionId, CompanyGroup(companyId));
        await base.OnConnectedAsync();
    }

    public static string CompanyGroup(int companyId) => $"company-{companyId}";
}
