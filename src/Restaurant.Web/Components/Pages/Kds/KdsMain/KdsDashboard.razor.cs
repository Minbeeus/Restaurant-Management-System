using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Restaurant.Application.DTOs;

namespace Restaurant.Web.Components.Pages.Kds.KdsMain;

public partial class KdsDashboard : IAsyncDisposable
{
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public HttpClient Http { get; set; } = default!;

    protected List<KitchenTicketDto> ActiveTickets { get; set; } = new();
    protected HashSet<int> ProcessingItems { get; set; } = new();

    protected bool IsLoading { get; set; } = true;
    protected string FilterStatus { get; set; } = "All";
    protected bool ShowCompletedOnly { get; set; } = false;
    protected bool NewTicketNotification { get; set; } = false;
    protected string SelectedStationId { get; set; } = string.Empty;
    protected List<KitchenStationDto> Stations { get; set; } = new();

    private class StationApiResponse
    {
        public bool Success { get; set; }
        public List<KitchenStationDto>? Data { get; set; }
    }

    private HubConnection? _hubConnection;

    protected int PendingCount => ActiveTickets.SelectMany(t => t.Items).Count(i => i.KitchenStatus == 0 && i.Status != 1);
    protected int ProcessingCount => ActiveTickets.SelectMany(t => t.Items).Count(i => i.KitchenStatus == 1 && i.Status != 1);

    protected List<KitchenTicketDto> DisplayedTickets
    {
        get
        {
            if (ShowCompletedOnly)
            {
                return ActiveTickets.Where(t => t.Items.All(i => i.KitchenStatus == 2 || i.Status == 1)).ToList();
            }

            var uncompleted = ActiveTickets.Where(t => t.Items.Any(i => i.KitchenStatus != 2 && i.Status != 1)).ToList();

            if (FilterStatus == "Pending")
                return uncompleted.Where(t => t.Items.Any(i => i.KitchenStatus == 0 && i.Status != 1)).ToList();
            if (FilterStatus == "Processing")
                return uncompleted.Where(t => t.Items.Any(i => i.KitchenStatus == 1 && i.Status != 1)).ToList();

            return uncompleted;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var res = await Http.GetFromJsonAsync<StationApiResponse>("api/v1/system/kitchen-stations");
            if (res?.Data != null && res.Data.Any())
            {
                Stations = res.Data.Where(s => s.IsActive).OrderBy(s => s.SortOrder).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading kitchen stations: {ex.Message}");
        }

        await LoadActiveTicketsAsync();

        // Hub Connection setup for Chefs with stationId query if present
        var hubUrl = string.IsNullOrWhiteSpace(SelectedStationId) 
            ? "http://localhost:5011/hubs/restaurant?clientType=kds" 
            : $"http://localhost:5011/hubs/restaurant?clientType=kds&stationId={SelectedStationId}";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.Reconnected += async (connectionId) =>
        {
            await LoadActiveTicketsAsync();
            await InvokeAsync(StateHasChanged);
        };

        _hubConnection.On<KitchenTicketDto>("ReceiveKitchenTicket", (ticket) =>
        {
            _ = InvokeAsync(async () =>
            {
                NewTicketNotification = true;
                await LoadActiveTicketsAsync();
                StateHasChanged();
            });
        });

        _hubConnection.On<int, string, int>("ReceiveItemStatusUpdate", (orderItemId, status, orderId) =>
        {
            _ = InvokeAsync(async () =>
            {
                await LoadActiveTicketsAsync();
                StateHasChanged();
            });
        });

        _hubConnection.On<int, int, int, int, string>("ReceiveOrderItemStatusChanged", (tableId, orderId, orderItemId, kitchenStatus, kitchenStatusName) =>
        {
            _ = InvokeAsync(async () =>
            {
                await LoadActiveTicketsAsync();
                StateHasChanged();
            });
        });

        try
        {
            await _hubConnection.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"KDS SignalR Error: {ex.Message}");
        }
    }

    protected async Task OnStationChangedAsync()
    {
        await LoadActiveTicketsAsync();
        if (_hubConnection != null && int.TryParse(SelectedStationId, out var sId) && sId > 0)
        {
            try
            {
                await _hubConnection.InvokeAsync("JoinStationGroup", sId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error joining station group: {ex.Message}");
            }
        }
    }

    private async Task LoadActiveTicketsAsync()
    {
        IsLoading = true;
        try
        {
            var url = $"api/v1/kds/tickets/active?includeCompleted={ShowCompletedOnly}";
            if (int.TryParse(SelectedStationId, out var parsedStationId) && parsedStationId > 0)
            {
                url += $"&stationId={parsedStationId}";
            }

            var tickets = await Http.GetFromJsonAsync<List<KitchenTicketDto>>(url);
            if (tickets != null)
            {
                ActiveTickets = tickets;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading tickets: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected void SetFilterStatus(string status) => FilterStatus = status;

    protected async Task ToggleCompletedViewAsync()
    {
        ShowCompletedOnly = !ShowCompletedOnly;
        await LoadActiveTicketsAsync();
    }

    protected async Task UpdateItemStatusAsync(int orderItemId, int newStatus)
    {
        if (ProcessingItems.Contains(orderItemId)) return;

        ProcessingItems.Add(orderItemId);

        try
        {
            var request = new UpdateKitchenStatusRequest { KitchenStatus = newStatus };
            var response = await Http.PatchAsJsonAsync($"api/v1/kds/items/{orderItemId}/status", request);

            if (response.IsSuccessStatusCode)
            {
                var updatedItem = await response.Content.ReadFromJsonAsync<KitchenItemDto>();
                if (updatedItem != null)
                {
                    foreach (var ticket in ActiveTickets)
                    {
                        var item = ticket.Items.FirstOrDefault(i => i.OrderItemId == orderItemId);
                        if (item != null)
                        {
                            item.KitchenStatus = updatedItem.KitchenStatus;
                            item.KitchenStatusName = updatedItem.KitchenStatusName;
                            
                            // If this was a child item, check and update its Combo Header in memory
                            if (item.ParentItemId.HasValue)
                            {
                                var header = ticket.Items.FirstOrDefault(i => i.OrderItemId == item.ParentItemId.Value);
                                if (header != null)
                                {
                                    var siblings = ticket.Items.Where(i => i.ParentItemId == item.ParentItemId.Value).ToList();
                                    if (siblings.All(s => s.KitchenStatus == 2))
                                    {
                                        header.KitchenStatus = 2;
                                        header.KitchenStatusName = "Done";
                                    }
                                    else if (siblings.Any(s => s.KitchenStatus == 1 || s.KitchenStatus == 2))
                                    {
                                        header.KitchenStatus = 1;
                                        header.KitchenStatusName = "Processing";
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
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

    protected async Task CompleteAllItemsInTicketAsync(KitchenTicketDto ticket)
    {
        foreach (var item in ticket.Items)
        {
            if (item.KitchenStatus != 2)
            {
                await UpdateItemStatusAsync(item.OrderItemId, 2);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
