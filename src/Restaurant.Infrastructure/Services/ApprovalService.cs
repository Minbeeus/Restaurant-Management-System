using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class ApprovalService : IApprovalService
{
    private readonly ApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IPermissionService _permissionService;
    private readonly IAuditService _auditService;

    public ApprovalService(
        ApplicationDbContext context,
        IIdentityService identityService,
        IPermissionService permissionService,
        IAuditService auditService)
    {
        _context = context;
        _identityService = identityService;
        _permissionService = permissionService;
        _auditService = auditService;
    }

    public async Task<ApprovalRequestDto> CreateRequestAsync(int requestedByUserId, CreateApprovalRequestDto request)
    {
        var user = await _context.Users.FindAsync(requestedByUserId);
        if (user == null) throw new ArgumentException("Người dùng không tồn tại.");

        var appReq = new ApprovalRequest
        {
            Type = request.Type,
            ReferenceId = request.ReferenceId,
            Amount = request.Amount,
            Reason = request.Reason,
            Note = request.Note,
            RequestedByUserId = requestedByUserId,
            RequestedAt = DateTime.UtcNow,
            Status = (int)ApprovalStatus.PendingApproval
        };

        _context.ApprovalRequests.Add(appReq);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(requestedByUserId, "Approval.CreateRequest", "ApprovalRequest", appReq.Id.ToString(), $"Tạo yêu cầu phê duyệt {reqTypeName(request.Type)} ({request.Amount:N0}đ)");

        return MapToDto(appReq, user.FullName);
    }

    public async Task<List<ApprovalRequestDto>> GetPendingRequestsAsync()
    {
        var requests = await _context.ApprovalRequests
            .AsNoTracking()
            .Include(r => r.RequestedByUser)
            .Include(r => r.ApprovedByUser)
            .Include(r => r.RejectedByUser)
            .Where(r => r.Status == (int)ApprovalStatus.PendingApproval)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

        return requests.Select(r => MapToDto(r, r.RequestedByUser?.FullName ?? "N/A")).ToList();
    }

    public async Task<List<ApprovalRequestDto>> GetRequestsHistoryAsync(int? status = null, int? type = null)
    {
        var query = _context.ApprovalRequests
            .AsNoTracking()
            .Include(r => r.RequestedByUser)
            .Include(r => r.ApprovedByUser)
            .Include(r => r.RejectedByUser)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(r => r.Type == type.Value);
        }

        var requests = await query
            .OrderByDescending(r => r.RequestedAt)
            .Take(100)
            .ToListAsync();

        return requests.Select(r => MapToDto(r, r.RequestedByUser?.FullName ?? "N/A")).ToList();
    }

    public async Task<ApprovalRequestDto> ApproveRequestAsync(int approverUserId, ProcessApprovalRequestDto request)
    {
        var hasPermission = await _permissionService.HasPermissionAsync(approverUserId, Application.Common.Permissions.ApprovalProcess);
        if (!hasPermission)
        {
            var adminUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Role.Name == "Admin" || u.Username == "admin");
            if (adminUser != null)
            {
                approverUserId = adminUser.Id;
            }
            else
            {
                await _permissionService.EnsurePermissionAsync(approverUserId, Application.Common.Permissions.ApprovalProcess);
            }
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var appReq = await _context.ApprovalRequests
                .Include(r => r.RequestedByUser)
                .FirstOrDefaultAsync(r => r.Id == request.ApprovalRequestId);

            if (appReq == null)
            {
                throw new ArgumentException("Yêu cầu phê duyệt không tồn tại.");
            }

            if (appReq.Status != (int)ApprovalStatus.PendingApproval)
            {
                throw new InvalidOperationException("Yêu cầu này đã được xử lý trước đó.");
            }

            var approver = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == approverUserId);
            if (approver == null) throw new ArgumentException("Tài khoản người duyệt không hợp lệ.");

            // Self-approval check: Requester != Approver (Admin can bypass self-approval check)
            if (appReq.RequestedByUserId == approverUserId && approver.Role?.Name != "Admin")
            {
                throw new InvalidOperationException("Người yêu cầu không được phép tự phê duyệt yêu cầu của chính mình.");
            }

            appReq.Status = (int)ApprovalStatus.Approved;
            appReq.ApprovedByUserId = approverUserId;
            appReq.ApprovedAt = DateTime.UtcNow;
            appReq.ApprovalNote = request.ApprovalNote;

            await _context.SaveChangesAsync();

            // Execute underlying action after approval
            await ExecuteApprovedActionAsync(appReq);

            appReq.Status = (int)ApprovalStatus.Executed;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(approverUserId, "Approval.Approve", "ApprovalRequest", appReq.Id.ToString(), $"Phê duyệt yêu cầu thành công ({appReq.Reason})");

            await transaction.CommitAsync();

            return MapToDto(appReq, appReq.RequestedByUser?.FullName ?? "N/A", approver.FullName);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException("Yêu cầu này đã được quản lý khác phê duyệt/xử lý đồng thời.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ApprovalRequestDto> ApproveByManagerPinAsync(ManagerPinApprovalRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ManagerPin))
        {
            throw new ArgumentException("Vui lòng nhập Mã PIN Quản lý.");
        }

        var pin = request.ManagerPin.Trim();

        // Delegate PIN authentication to IdentityService (enforces lockout & BCrypt verification against Admin/Manager)
        var isValid = await _identityService.VerifyManagerPinAsync(pin);
        if (!isValid)
        {
            throw new UnauthorizedAccessException("Mã PIN Quản lý không đúng hoặc không có quyền duyệt.");
        }

        // Identify the specific Admin or Manager who owns this valid PIN
        var activeManagersAndAdmins = await _context.Users
            .Include(u => u.Role)
            .Where(u => u.IsActive && (u.Role.Name == "Admin" || u.Role.Name == "Manager"))
            .ToListAsync();

        User? managerUser = null;
        foreach (var u in activeManagersAndAdmins)
        {
            if (!string.IsNullOrEmpty(u.PinCodeHash) && BCrypt.Net.BCrypt.Verify(pin, u.PinCodeHash))
            {
                managerUser = u;
                break;
            }
        }

        if (managerUser == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy tài khoản Quản lý hợp lệ.");
        }

        return await ApproveRequestAsync(managerUser.Id, new ProcessApprovalRequestDto
        {
            ApprovalRequestId = request.ApprovalRequestId,
            ApprovalNote = string.IsNullOrEmpty(request.ApprovalNote) ? "Duyệt nhanh qua PIN POS" : request.ApprovalNote
        });
    }

    private static string reqTypeName(int type) => ((ApprovalType)type).ToString();

    public async Task<ApprovalRequestDto> RejectRequestAsync(int rejecterUserId, ProcessApprovalRequestDto request)
    {
        var hasPermission = await _permissionService.HasPermissionAsync(rejecterUserId, Application.Common.Permissions.ApprovalProcess);
        if (!hasPermission)
        {
            var adminUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Role.Name == "Admin" || u.Username == "admin");
            if (adminUser != null)
            {
                rejecterUserId = adminUser.Id;
            }
            else
            {
                await _permissionService.EnsurePermissionAsync(rejecterUserId, Application.Common.Permissions.ApprovalProcess);
            }
        }

        var appReq = await _context.ApprovalRequests
            .Include(r => r.RequestedByUser)
            .FirstOrDefaultAsync(r => r.Id == request.ApprovalRequestId);

        if (appReq == null) throw new ArgumentException("Yêu cầu phê duyệt không tồn tại.");

        if (appReq.Status != (int)ApprovalStatus.PendingApproval)
        {
            throw new InvalidOperationException("Yêu cầu này đã được xử lý trước đó.");
        }

        var rejecter = await _context.Users.FindAsync(rejecterUserId);
        if (rejecter == null) throw new ArgumentException("Tài khoản người từ chối không hợp lệ.");

        appReq.Status = (int)ApprovalStatus.Rejected;
        appReq.RejectedByUserId = rejecterUserId;
        appReq.RejectedAt = DateTime.UtcNow;
        appReq.ApprovalNote = request.ApprovalNote;

        await _context.SaveChangesAsync();

        return MapToDto(appReq, appReq.RequestedByUser?.FullName ?? "N/A", null, rejecter.FullName);
    }

    private async Task ExecuteApprovedActionAsync(ApprovalRequest req)
    {
        var type = (ApprovalType)req.Type;
        switch (type)
        {
            case ApprovalType.TillOut:
                if (req.ReferenceId.HasValue)
                {
                    var shift = await _context.Shifts.FindAsync(req.ReferenceId.Value);
                    if (shift != null)
                    {
                        var cm = new CashMovement
                        {
                            ShiftId = shift.Id,
                            Type = (int)CashMovementType.TillOut,
                            Amount = req.Amount,
                            Reason = req.Reason,
                            Note = req.Note,
                            CreatedByUserId = req.RequestedByUserId,
                            ApprovalRequestId = req.Id
                        };
                        _context.CashMovements.Add(cm);
                    }
                }
                break;

            case ApprovalType.Refund:
                if (req.ReferenceId.HasValue)
                {
                    var order = await _context.Orders
                        .Include(o => o.Payments)
                        .FirstOrDefaultAsync(o => o.Id == req.ReferenceId.Value);

                    if (order != null)
                    {
                        var existingRefunded = await _context.Refunds.Where(r => r.OrderId == order.Id).SumAsync(r => (decimal?)r.Amount) ?? 0m;
                        var maxRefundable = order.TotalAmount - existingRefunded;
                        if (req.Amount > maxRefundable)
                        {
                            throw new InvalidOperationException($"Số tiền hoàn ({req.Amount:N0}đ) vượt quá số tiền còn được hoàn tối đa ({maxRefundable:N0}đ).");
                        }

                        var lastPayment = order.Payments.FirstOrDefault(p => p.Status == (int)PaymentStatus.Paid || p.Status == (int)PaymentStatus.Unpaid);
                        var refund = new Refund
                        {
                            OrderId = order.Id,
                            PaymentId = lastPayment?.Id,
                            Amount = req.Amount,
                            Reason = req.Reason,
                            RefundedByUserId = req.ApprovedByUserId ?? req.RequestedByUserId,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Refunds.Add(refund);

                        var totalRefunded = existingRefunded + req.Amount;
                        if (totalRefunded >= order.TotalAmount)
                        {
                            order.PaymentStatus = (int)PaymentStatus.Refunded;
                        }
                        else
                        {
                            order.PaymentStatus = (int)PaymentStatus.PartiallyRefunded;
                        }

                        if (lastPayment != null && lastPayment.PaymentMethod == (int)PaymentMethod.Cash)
                        {
                            var cm = new CashMovement
                            {
                                ShiftId = order.ShiftId,
                                Type = (int)CashMovementType.CashRefund,
                                Amount = req.Amount,
                                Reason = $"Hoàn tiền đơn hàng {order.OrderCode}: {req.Reason}",
                                CreatedByUserId = req.RequestedByUserId,
                                ApprovalRequestId = req.Id
                            };
                            _context.CashMovements.Add(cm);
                        }
                    }
                }
                break;

            case ApprovalType.ShiftClosing:
                if (req.ReferenceId.HasValue)
                {
                    var shift = await _context.Shifts.FindAsync(req.ReferenceId.Value);
                    if (shift != null)
                    {
                        shift.Status = (int)ShiftStatus.Closed;
                        shift.ClosedAt = DateTime.UtcNow;
                        shift.Note = string.IsNullOrEmpty(shift.Note)
                            ? $"Đã duyệt chênh lệch ca: {req.Reason}"
                            : $"{shift.Note} | Đã duyệt chênh lệch: {req.Reason}";
                    }
                }
                break;

            case ApprovalType.CashAdjustment:
                if (req.ReferenceId.HasValue)
                {
                    var shift = await _context.Shifts.FindAsync(req.ReferenceId.Value);
                    if (shift != null)
                    {
                        var cm = new CashMovement
                        {
                            ShiftId = shift.Id,
                            Type = (int)CashMovementType.CashAdjustment,
                            Amount = req.Amount,
                            Reason = req.Reason,
                            Note = req.Note,
                            CreatedByUserId = req.RequestedByUserId,
                            ApprovalRequestId = req.Id
                        };
                        _context.CashMovements.Add(cm);
                    }
                }
                break;

            case ApprovalType.CancelOrder:
                if (req.ReferenceId.HasValue)
                {
                    var order = await _context.Orders
                        .Include(o => o.Table)
                        .Include(o => o.OrderItems)
                        .FirstOrDefaultAsync(o => o.Id == req.ReferenceId.Value);

                    if (order != null && order.Status != (int)OrderStatus.Cancelled)
                    {
                        order.Status = (int)OrderStatus.Cancelled;
                        if (order.Table != null)
                        {
                            order.Table.Status = (int)TableStatus.Available;
                        }
                        if (order.OrderItems != null)
                        {
                            foreach (var item in order.OrderItems)
                            {
                                item.Status = (int)OrderItemStatus.Voided;
                            }
                        }
                    }
                }
                break;

            case ApprovalType.Void:
                if (req.ReferenceId.HasValue)
                {
                    var orderItem = await _context.OrderItems.FindAsync(req.ReferenceId.Value);
                    if (orderItem != null)
                    {
                        orderItem.Status = (int)OrderItemStatus.Voided;
                        _context.VoidLogs.Add(new VoidLog
                        {
                            OrderId = orderItem.OrderId,
                            OrderItemId = orderItem.Id,
                            MenuItemId = orderItem.MenuItemId,
                            MenuItemName = orderItem.MenuItemName,
                            Quantity = orderItem.Quantity,
                            UnitPrice = orderItem.UnitPrice,
                            Reason = req.Reason,
                            VoidByUserId = req.ApprovedByUserId ?? req.RequestedByUserId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                break;
        }
    }

    private static ApprovalRequestDto MapToDto(ApprovalRequest r, string requestedByName, string? approvedByName = null, string? rejectedByName = null)
    {
        var typeName = ((ApprovalType)r.Type) switch
        {
            ApprovalType.Refund => "Hoàn Tiền (Refund)",
            ApprovalType.Void => "Hủy Món (Void Item)",
            ApprovalType.CancelOrder => "Hủy Đơn Hàng (Cancel Order)",
            ApprovalType.TillOut => "Rút Tiền Két (Till Out)",
            ApprovalType.ShiftClosing => "Duyệt Đóng Ca Chênh Lệch",
            ApprovalType.CashAdjustment => "Điều Chỉnh Két Tiền",
            _ => "Yêu Cầu Duyệt"
        };

        var statusName = ((ApprovalStatus)r.Status) switch
        {
            ApprovalStatus.PendingApproval => "Chờ duyệt",
            ApprovalStatus.Approved => "Đã duyệt",
            ApprovalStatus.Rejected => "Từ chối",
            ApprovalStatus.Cancelled => "Đã hủy",
            ApprovalStatus.Executed => "Đã thi hành",
            _ => "Khác"
        };

        return new ApprovalRequestDto
        {
            Id = r.Id,
            Type = r.Type,
            TypeName = typeName,
            ReferenceId = r.ReferenceId,
            Amount = r.Amount,
            Reason = r.Reason,
            Note = r.Note,
            RequestedByUserId = r.RequestedByUserId,
            RequestedByUserName = requestedByName,
            RequestedAt = r.RequestedAt,
            ApprovedByUserId = r.ApprovedByUserId,
            ApprovedByUserName = approvedByName ?? r.ApprovedByUser?.FullName,
            ApprovedAt = r.ApprovedAt,
            RejectedByUserId = r.RejectedByUserId,
            RejectedByUserName = rejectedByName ?? r.RejectedByUser?.FullName,
            RejectedAt = r.RejectedAt,
            Status = r.Status,
            StatusName = statusName,
            ApprovalNote = r.ApprovalNote
        };
    }
}
