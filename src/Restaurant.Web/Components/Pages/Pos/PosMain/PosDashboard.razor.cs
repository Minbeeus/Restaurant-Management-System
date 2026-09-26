using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Restaurant.Application.DTOs;

namespace Restaurant.Web.Components.Pages.Pos.PosMain;

public partial class PosDashboard : IAsyncDisposable
{
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public HttpClient Http { get; set; } = default!;

    protected List<AreaDto> Areas { get; set; } = new();
    protected List<TableStatusOverviewDto> Tables { get; set; } = new();

    protected int SelectedAreaId { get; set; } = 0;
    protected bool IsLoading { get; set; } = true;

    protected bool ShowMoveMergeModal { get; set; } = false;
    protected bool IsMergeMode { get; set; } = false;
    protected int SourceTableId { get; set; } = 0;
    protected int TargetTableId { get; set; } = 0;
    protected string ModalErrorMessage { get; set; } = string.Empty;

    private HubConnection? _hubConnection;

    protected List<TableStatusOverviewDto> FilteredTables =>
        SelectedAreaId == 0 ? Tables : Tables.Where(t => t.AreaId == SelectedAreaId).ToList();

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();

        // Setup SignalR live connection
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5011/hubs/restaurant")
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string, string>("TableStatusChanged", (tableId, status) =>
        {
            _ = InvokeAsync(async () =>
            {
                await LoadTablesAsync();
                StateHasChanged();
            });
        });

        _hubConnection.On<string, string>("NewQrOrderReceived", (tableId, tableName) =>
        {
            _ = InvokeAsync(async () =>
            {
                await LoadTablesAsync();
                StateHasChanged();
            });
        });

        _hubConnection.On<string>("OrderPaid", (orderId) =>
        {
            _ = InvokeAsync(async () =>
            {
                await LoadTablesAsync();
                StateHasChanged();
            });
        });

        try
        {
            await _hubConnection.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR Connection Error: {ex.Message}");
        }
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var areasTask = Http.GetFromJsonAsync<List<AreaDto>>("api/v1/areas");
            var tablesTask = Http.GetFromJsonAsync<ApiResponseWrapper<List<TableStatusOverviewDto>>>("api/v1/pos/tables/overview");

            await Task.WhenAll(areasTask, tablesTask);

            Areas = await areasTask ?? new List<AreaDto>();
            var tablesResult = await tablesTask;
            Tables = tablesResult?.Data ?? new List<TableStatusOverviewDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading dashboard data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadTablesAsync()
    {
        try
        {
            var tablesResult = await Http.GetFromJsonAsync<ApiResponseWrapper<List<TableStatusOverviewDto>>>("api/v1/pos/tables/overview");
            Tables = tablesResult?.Data ?? new List<TableStatusOverviewDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating tables: {ex.Message}");
        }
    }

    protected void SelectArea(int areaId)
    {
        SelectedAreaId = areaId;
    }

    protected void HandleTableClick(TableStatusOverviewDto table)
    {
        Navigation.NavigateTo($"/pos/order?tableId={table.TableId}");
    }

    protected void CreateTakeawayOrder()
    {
        Navigation.NavigateTo("/pos/order?isTakeaway=true");
    }

    protected void OpenMoveModal()
    {
        IsMergeMode = false;
        SourceTableId = 0;
        TargetTableId = 0;
        ModalErrorMessage = string.Empty;
        ShowMoveMergeModal = true;
    }

    protected void OpenMergeModal()
    {
        IsMergeMode = true;
        SourceTableId = 0;
        TargetTableId = 0;
        ModalErrorMessage = string.Empty;
        ShowMoveMergeModal = true;
    }

    protected async Task ExecuteMoveMergeAsync()
    {
        if (SourceTableId == 0 || TargetTableId == 0)
        {
            ModalErrorMessage = "Vui lòng chọn đầy đủ bàn nguồn và bàn đích.";
            return;
        }

        ModalErrorMessage = string.Empty;

        try
        {
            HttpResponseMessage response;
            if (IsMergeMode)
            {
                var req = new MergeTableRequest
                {
                    SourceTableIds = new List<int> { SourceTableId },
                    TargetTableId = TargetTableId
                };
                response = await Http.PostAsJsonAsync("api/v1/pos/tables/merge", req);
            }
            else
            {
                var req = new MoveTableRequest
                {
                    SourceTableId = SourceTableId,
                    TargetTableId = TargetTableId
                };
                response = await Http.PostAsJsonAsync("api/v1/pos/tables/move", req);
            }

            if (response.IsSuccessStatusCode)
            {
                ShowMoveMergeModal = false;
                await LoadTablesAsync();
            }
            else
            {
                var err = await response.Content.ReadAsStringAsync();
                ModalErrorMessage = $"Thao tác thất bại: {err}";
            }
        }
        catch (Exception ex)
        {
            ModalErrorMessage = ex.Message;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }

    private class ApiResponseWrapper<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }
}
