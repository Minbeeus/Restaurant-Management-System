using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/vouchers")]
[Authorize(Roles = "Admin")]
public class VouchersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public VouchersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("validate")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateVoucher([FromQuery] string code, [FromQuery] decimal orderAmount)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Ok(new VoucherValidationResponseDto 
            { 
                IsValid = false, 
                Message = "Mã voucher không được để trống.", 
                DiscountAmount = 0 
            });
        }

        var normalizedCode = code.Trim().ToUpper();
        var voucher = await _context.Vouchers
            .FirstOrDefaultAsync(v => v.Code.ToUpper() == normalizedCode);

        if (voucher == null)
        {
            return Ok(new VoucherValidationResponseDto 
            { 
                IsValid = false, 
                Message = "Mã giảm giá không tồn tại.", 
                Code = code 
            });
        }

        if (!voucher.IsActive)
        {
            return Ok(new VoucherValidationResponseDto 
            { 
                IsValid = false, 
                Message = "Mã giảm giá đã bị khóa hoặc ngưng áp dụng.", 
                Code = code 
            });
        }

        var now = DateTime.UtcNow;
        if (voucher.StartDate > now)
        {
            return Ok(new VoucherValidationResponseDto 
            { 
                IsValid = false, 
                Message = "Mã giảm giá chưa đến đợt áp dụng.", 
                Code = code 
            });
        }

        if (voucher.EndDate < now)
        {
            return Ok(new VoucherValidationResponseDto 
            { 
                IsValid = false, 
                Message = "Mã giảm giá đã hết hạn sử dụng.", 
                Code = code 
            });
        }

        if (voucher.UsedCount >= voucher.UsageLimit)
        {
            return Ok(new VoucherValidationResponseDto 
            { 
                IsValid = false, 
                Message = "Mã giảm giá đã hết lượt sử dụng.", 
                Code = code 
            });
        }

        if (voucher.MinOrderAmount.HasValue && orderAmount < voucher.MinOrderAmount.Value)
        {
            return Ok(new VoucherValidationResponseDto 
            { 
                IsValid = false, 
                Message = $"Đơn hàng tối thiểu để dùng voucher này là {voucher.MinOrderAmount.Value:N0}đ.", 
                Code = code 
            });
        }

        decimal discountAmount = 0;
        string discountTypeStr = "Percentage";
        if (voucher.DiscountType == 1 || voucher.DiscountType == 0) // Percentage
        {
            discountTypeStr = "Percentage";
            discountAmount = orderAmount * (voucher.DiscountValue / 100m);
            if (voucher.MaxDiscountAmount.HasValue && voucher.MaxDiscountAmount.Value > 0)
            {
                discountAmount = Math.Min(discountAmount, voucher.MaxDiscountAmount.Value);
            }
        }
        else // FixedAmount (DiscountType == 2)
        {
            discountTypeStr = "FixedAmount";
            discountAmount = Math.Min(voucher.DiscountValue, orderAmount);
        }

        return Ok(new VoucherValidationResponseDto
        {
            IsValid = true,
            Message = $"Áp dụng voucher {voucher.Code} thành công!",
            Code = voucher.Code,
            DiscountType = discountTypeStr,
            DiscountValue = voucher.DiscountValue,
            DiscountAmount = Math.Round(discountAmount, 0)
        });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetVouchers()
    {
        var vouchers = await _context.Vouchers.AsNoTracking().ToListAsync();
        return Ok(new { success = true, data = vouchers });
    }

    [HttpPost]
    public async Task<IActionResult> CreateVoucher([FromBody] CreateVoucherRequest request)
    {
        var existing = await _context.Vouchers.AnyAsync(v => v.Code.ToUpper() == request.Code.Trim().ToUpper());
        if (existing)
        {
            return BadRequest(new { success = false, message = "Mã voucher đã tồn tại." });
        }

        var voucher = new Restaurant.Domain.Entities.Voucher
        {
            Code = request.Code.Trim().ToUpper(),
            DiscountType = request.DiscountType == "FixedAmount" ? 2 : 1,
            DiscountValue = request.DiscountValue,
            MinOrderAmount = request.MinOrderAmount,
            MaxDiscountAmount = request.MaxDiscountAmount,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            UsageLimit = request.UsageLimit > 0 ? request.UsageLimit : 100,
            UsedCount = 0,
            IsActive = true
        };

        _context.Vouchers.Add(voucher);
        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = voucher });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVoucher(int id, [FromBody] CreateVoucherRequest request)
    {
        var voucher = await _context.Vouchers.FindAsync(id);
        if (voucher == null) return NotFound();

        voucher.DiscountType = request.DiscountType == "FixedAmount" ? 2 : 1;
        voucher.DiscountValue = request.DiscountValue;
        voucher.MinOrderAmount = request.MinOrderAmount;
        voucher.MaxDiscountAmount = request.MaxDiscountAmount;
        voucher.StartDate = request.StartDate;
        voucher.EndDate = request.EndDate;
        voucher.UsageLimit = request.UsageLimit;
        voucher.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return Ok(new { success = true, data = voucher });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVoucher(int id)
    {
        var voucher = await _context.Vouchers.FindAsync(id);
        if (voucher == null) return NotFound();

        voucher.IsActive = false;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Đã ngưng áp dụng voucher." });
    }
}
