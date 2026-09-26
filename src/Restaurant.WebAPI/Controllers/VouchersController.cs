using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.WebAPI.Controllers;

[ApiController]
[Route("api/v1/vouchers")]
[AllowAnonymous]
public class VouchersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public VouchersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("validate")]
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
    public async Task<IActionResult> GetVouchers()
    {
        var vouchers = await _context.Vouchers.AsNoTracking().ToListAsync();
        return Ok(new { success = true, data = vouchers });
    }
}
