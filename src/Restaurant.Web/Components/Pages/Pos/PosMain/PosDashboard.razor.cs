using Microsoft.AspNetCore.Components;

namespace Restaurant.Web.Components.Pages.Pos.PosMain;

public partial class PosDashboard
{
    protected List<TableViewModel> MockTables { get; set; } = new();

    protected override void OnInitialized()
    {
        // Mock data initialization
        MockTables = new List<TableViewModel>
        {
            new TableViewModel { Id = 1, Name = "Bàn 101", Capacity = 4, Status = "Available", TotalAmount = 0 },
            new TableViewModel { Id = 2, Name = "Bàn 102", Capacity = 4, Status = "Occupied", TotalAmount = 450000 },
            new TableViewModel { Id = 3, Name = "Bàn 103", Capacity = 2, Status = "Available", TotalAmount = 0 },
            new TableViewModel { Id = 4, Name = "Bàn 104", Capacity = 6, Status = "NewOrder", TotalAmount = 0 },
            new TableViewModel { Id = 5, Name = "Bàn 105", Capacity = 4, Status = "Billing", TotalAmount = 1250000 },
            new TableViewModel { Id = 6, Name = "Bàn 106", Capacity = 4, Status = "Occupied", TotalAmount = 250000 },
            new TableViewModel { Id = 7, Name = "Bàn 107", Capacity = 8, Status = "Available", TotalAmount = 0 },
            new TableViewModel { Id = 8, Name = "Bàn 108", Capacity = 2, Status = "Available", TotalAmount = 0 },
        };
    }

    protected void HandleTableClick(int tableId)
    {
        Console.WriteLine($"Clicked on table {tableId}");
        // TODO: Mở modal chọn món hoặc chi tiết bàn
    }

    public class TableViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }
}
