using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Restaurant.Application.DTOs;

namespace Restaurant.Web.Components.Pages.Kds.KdsMain;

public partial class KdsDashboard : IAsyncDisposable
{
    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    [Inject]
    public HttpClient Http { get; set; } = default!;

    protected List<KitchenTicketDto> ActiveTickets { get; set; } = new();
    protected HashSet<int> ProcessingItems { get; set; } = new();
    
    private HubConnection? hubConnection;

    protected override async Task OnInitializedAsync()
    {
        await LoadActiveTickets();

        // Hub Connection setup for Chefs
        hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/hubs/restaurant"))
            .Build();

        hubConnection.On<KitchenTicketDto>("ReceiveKitchenTicket", (ticket) =>
        {
            try
            {
                // Append ticket to UI
                Console.WriteLine($"Received ticket data for Order: {ticket.OrderCode}");
                
                // Check if ticket already exists (might be appending items)
                var existing = ActiveTickets.FirstOrDefault(t => t.OrderId == ticket.OrderId);
                if (existing != null)
                {
                    existing.Items.AddRange(ticket.Items);
                }
                else
                {
                    ActiveTickets.Add(ticket);
                }
                
                InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing ReceiveKitchenTicket: {ex.Message}");
            }
        });

        await hubConnection.StartAsync();
    }

    private async Task LoadActiveTickets()
    {
        try
        {
            var tickets = await Http.GetFromJsonAsync<List<KitchenTicketDto>>("api/v1/kds/tickets/active");
            if (tickets != null)
            {
                ActiveTickets = tickets;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading tickets: {ex.Message}");
        }
    }

    protected async Task UpdateItemStatus(int orderItemId, int newStatus)
    {
        if (ProcessingItems.Contains(orderItemId)) return;
        
        Console.WriteLine($"Chef updated item {orderItemId} to status {newStatus}");
        ProcessingItems.Add(orderItemId);
        
        try
        {
            var request = new UpdateKitchenStatusRequest { KitchenStatus = newStatus };
            var response = await Http.PatchAsJsonAsync($"api/v1/kds/items/{orderItemId}/status", request);
            
            if (response.IsSuccessStatusCode)
            {
                // Find item and update locally
                foreach (var ticket in ActiveTickets)
                {
                    var item = ticket.Items.FirstOrDefault(i => i.OrderItemId == orderItemId);
                    if (item != null)
                    {
                        item.KitchenStatus = newStatus;
                        item.KitchenStatusName = newStatus == 1 ? "Processing" : (newStatus == 2 ? "Done" : "Pending");
                        break;
                    }
                }
            }
            else
            {
                Console.WriteLine("Failed to update status.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating item status: {ex.Message}");
        }
        finally
        {
            ProcessingItems.Remove(orderItemId);
            await InvokeAsync(StateHasChanged);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }
    }
}
