using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/system")]
[Authorize(Roles = "Admin,Manager")]
public class SystemController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private static SystemSettingsDto _currentSettings = new SystemSettingsDto();

    public SystemController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("roles")]
    public async Task<ActionResult> GetRoles()
    {
        var roles = await _context.Roles
            .Include(r => r.Users)
            .OrderBy(r => r.Id)
            .ToListAsync();

        if (!roles.Any())
        {
            var defaultRoles = new List<Role>
            {
                new Role { Name = "Admin", Description = "Quản trị viên toàn quyền hệ thống" },
                new Role { Name = "Manager", Description = "Quản lý ca, duyệt hủy món, quản lý kho" },
                new Role { Name = "Cashier", Description = "Thu ngân, thanh toán và in hóa đơn" },
                new Role { Name = "Chef", Description = "Bếp trưởng và nhân viên bếp, điều phối KDS" },
                new Role { Name = "Staff", Description = "Nhân viên phục vụ, order tại bàn" }
            };
            await _context.Roles.AddRangeAsync(defaultRoles);
            await _context.SaveChangesAsync();
            roles = defaultRoles;
        }

        var dtos = roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description,
            UserCount = r.Users?.Count ?? 0,
            Permissions = GetRolePermissions(r.Name)
        }).ToList();

        return Ok(new { success = true, data = dtos });
    }

    [HttpGet("registers")]
    public async Task<ActionResult> GetRegisters()
    {
        var registers = await _context.Registers
            .OrderBy(r => r.Code)
            .ToListAsync();

        if (!registers.Any())
        {
            var defaults = new List<Register>
            {
                new Register { Code = "POS-01", Name = "Quầy Thu Ngân Chính (Tầng 1)", IsActive = true },
                new Register { Code = "POS-02", Name = "Quầy Thu Ngân Bar & Đồ Uống", IsActive = true },
                new Register { Code = "POS-03", Name = "Máy POS Di Động / Mang Về", IsActive = true }
            };
            await _context.Registers.AddRangeAsync(defaults);
            await _context.SaveChangesAsync();
            registers = defaults;
        }

        var dtos = registers.Select(r => new RegisterDto
        {
            Id = r.Id,
            Code = r.Code,
            Name = r.Name,
            IsActive = r.IsActive,
            CreatedAt = r.CreatedAt
        }).ToList();

        return Ok(new { success = true, data = dtos });
    }

    [HttpPost("registers")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CreateRegister([FromBody] CreateRegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { success = false, message = "Mã máy và Tên máy không được để trống" });
        }

        var exists = await _context.Registers.AnyAsync(r => r.Code == request.Code.Trim());
        if (exists)
        {
            return BadRequest(new { success = false, message = $"Mã máy '{request.Code}' đã tồn tại" });
        }

        var reg = new Register
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            IsActive = request.IsActive
        };

        _context.Registers.Add(reg);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, data = reg, message = "Thêm máy POS mới thành công" });
    }

    [HttpPut("registers/{id}/toggle-status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ToggleRegisterStatus(int id)
    {
        var reg = await _context.Registers.FindAsync(id);
        if (reg == null)
            return NotFound(new { success = false, message = "Không tìm thấy máy POS" });

        reg.IsActive = !reg.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, data = reg.IsActive, message = $"Đã {(reg.IsActive ? "kích hoạt" : "vô hiệu hóa")} máy {reg.Code}" });
    }

    [HttpGet("kitchen-stations")]
    public async Task<ActionResult> GetKitchenStations()
    {
        var stations = await _context.KitchenStations
            .Include(s => s.MenuItems)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        var dtos = stations.Select(s => new KitchenStationDto
        {
            Id = s.Id,
            Code = s.Code,
            Name = s.Name,
            SortOrder = s.SortOrder,
            IsActive = s.IsActive,
            MenuItemCount = s.MenuItems?.Count ?? 0
        }).ToList();

        return Ok(new { success = true, data = dtos });
    }

    [HttpPost("kitchen-stations")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> CreateKitchenStation([FromBody] CreateKitchenStationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { success = false, message = "Mã khu vực và tên khu vực không được để trống" });
        }

        var exists = await _context.KitchenStations.AnyAsync(k => k.Code == request.Code.Trim());
        if (exists)
        {
            return BadRequest(new { success = false, message = $"Mã khu vực '{request.Code}' đã tồn tại" });
        }

        var station = new KitchenStation
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };

        _context.KitchenStations.Add(station);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, data = station, message = "Tạo khu vực bếp thành công" });
    }

    [HttpPut("kitchen-stations/{id}/toggle-status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ToggleKitchenStationStatus(int id)
    {
        var station = await _context.KitchenStations.FindAsync(id);
        if (station == null)
            return NotFound(new { success = false, message = "Không tìm thấy khu vực bếp" });

        station.IsActive = !station.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, data = station.IsActive, message = $"Đã {(station.IsActive ? "kích hoạt" : "vô hiệu hóa")} khu vực {station.Name}" });
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult> GetAuditLogs()
    {
        var logs = await _context.AuditLogEntries
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .Take(100)
            .ToListAsync();

        if (!logs.Any())
        {
            var adminUser = await _context.Users.FirstOrDefaultAsync();
            if (adminUser != null)
            {
                var initialLog = new AuditLogEntry
                {
                    UserId = adminUser.Id,
                    Action = "System.Init",
                    EntityName = "System",
                    EntityId = "1",
                    Details = "Khởi tạo hệ thống và cấu hình mặc định BOH",
                    IpAddress = "127.0.0.1"
                };
                _context.AuditLogEntries.Add(initialLog);
                await _context.SaveChangesAsync();
                logs = new List<AuditLogEntry> { initialLog };
            }
        }

        var dtos = logs.Select(l => new AuditLogDto
        {
            Id = l.Id,
            UserId = l.UserId,
            UserName = l.User?.FullName ?? l.User?.Username ?? "System",
            Action = l.Action,
            EntityName = l.EntityName,
            EntityId = l.EntityId,
            Details = l.Details,
            IpAddress = l.IpAddress,
            CreatedAt = l.CreatedAt
        }).ToList();

        return Ok(new { success = true, data = dtos });
    }

    [HttpGet("settings")]
    public ActionResult GetSettings()
    {
        return Ok(new { success = true, data = _currentSettings });
    }

    [HttpPost("settings")]
    [Authorize(Roles = "Admin")]
    public ActionResult SaveSettings([FromBody] SystemSettingsDto settings)
    {
        _currentSettings = settings ?? new SystemSettingsDto();
        return Ok(new { success = true, data = _currentSettings, message = "Cập nhật cấu hình hệ thống thành công" });
    }

    private static List<string> GetRolePermissions(string roleName)
    {
        return roleName.ToUpperInvariant() switch
        {
            "ADMIN" => new List<string> { "Toàn quyền quản trị", "Quản lý nhân sự & tài khoản", "Xem & phân bổ kho hàng", "Cấu hình chính sách POS & BOH", "Xem báo cáo doanh thu & tài chính", "Phê duyệt các yêu cầu hủy món" },
            "MANAGER" => new List<string> { "Quản lý ca làm việc", "Duyệt void/cancel món", "Nhập kho & kiểm kê kho", "Theo dõi chênh lệch & thất thoát", "Xem báo cáo bán hàng theo ngày", "Xem danh sách nhân viên" },
            "CASHIER" => new List<string> { "Mở ca & Đóng ca thu ngân", "Tạo đơn hàng tại quầy", "Thanh toán tiền mặt / SePay QR", "In phiếu tạm tính & hóa đơn VAT", "Áp dụng voucher khuyến mãi" },
            "CHEF" => new List<string> { "Xem màn hình KDS", "Đổi trạng thái món (Chuẩn bị / Hoàn thành)", "Yêu cầu hủy hoặc hết món bếp", "Xem định lượng món ăn (BOM)" },
            _ => new List<string> { "Nhận order tại bàn", "Xem trạng thái bàn", "In phiếu bếp" }
        };
    }
}
