using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Restaurant.Application.Interfaces;

namespace Restaurant.Infrastructure.Hubs;

[AllowAnonymous]
public class RestaurantHub : Hub<IRestaurantHubClient>
{
    public const string CashiersGroup = "CashiersGroup";
    public const string ChefsGroup = "ChefsGroup";

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);

        if (!string.IsNullOrEmpty(role))
        {
            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("Manager", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("Cashier", StringComparison.OrdinalIgnoreCase))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, CashiersGroup);
            }

            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                role.Equals("Chef", StringComparison.OrdinalIgnoreCase))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, ChefsGroup);
            }
        }

        // Check if QR Client specified a tableId in query string
        var tableIdQuery = httpContext?.Request.Query["tableId"].ToString();
        if (int.TryParse(tableIdQuery, out var tableId) && tableId > 0)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"TableGroup_{tableId}");
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinTableGroup(int tableId)
    {
        if (tableId > 0)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"TableGroup_{tableId}");
        }
    }
}
