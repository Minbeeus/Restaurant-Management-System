using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Resources;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

public class LoyaltyService : ILoyaltyService
{
    private readonly ApplicationDbContext _context;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public LoyaltyService(ApplicationDbContext context, IStringLocalizer<SharedResources> localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<CustomerDto?> GetCustomerByPhoneAsync(string phoneNumber)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .Include(c => c.Tier)
            .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber && !c.IsDeleted);

        if (customer == null) return null;

        return new CustomerDto
        {
            Id = customer.Id,
            PhoneNumber = customer.PhoneNumber,
            FullName = customer.FullName,
            TierId = customer.TierId,
            TierName = customer.Tier?.Name ?? "Standard",
            TotalPoints = customer.TotalPoints,
            AvailablePoints = customer.AvailablePoints
        };
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request)
    {
        var existing = await _context.Customers
            .FirstOrDefaultAsync(c => c.PhoneNumber == request.PhoneNumber);

        if (existing != null)
        {
            return new CustomerDto
            {
                Id = existing.Id,
                PhoneNumber = existing.PhoneNumber,
                FullName = existing.FullName,
                TierId = existing.TierId,
                TierName = "Standard",
                TotalPoints = existing.TotalPoints,
                AvailablePoints = existing.AvailablePoints
            };
        }

        var defaultTier = await _context.CustomerTiers.FirstOrDefaultAsync() 
                          ?? new CustomerTier { Name = "Bronze", MinPoints = 0, DiscountPercent = 0 };

        var customer = new Customer
        {
            PhoneNumber = request.PhoneNumber,
            FullName = request.FullName,
            TierId = defaultTier.Id > 0 ? defaultTier.Id : 1,
            TotalPoints = 0,
            AvailablePoints = 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return new CustomerDto
        {
            Id = customer.Id,
            PhoneNumber = customer.PhoneNumber,
            FullName = customer.FullName,
            TierId = customer.TierId,
            TierName = defaultTier.Name,
            TotalPoints = 0,
            AvailablePoints = 0
        };
    }

    public async Task AccumulatePointsAsync(int customerId, int orderId, decimal paidAmount)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer == null) return;

        // 10,000 VND = 1 Point
        int pointsGained = (int)(paidAmount / 10000);
        if (pointsGained <= 0) return;

        customer.TotalPoints += pointsGained;
        customer.AvailablePoints += pointsGained;

        // Tier promotion check
        var eligibleTier = await _context.CustomerTiers
            .Where(t => customer.TotalPoints >= t.MinPoints)
            .OrderByDescending(t => t.MinPoints)
            .FirstOrDefaultAsync();

        if (eligibleTier != null)
        {
            customer.TierId = eligibleTier.Id;
        }

        _context.LoyaltyTransactions.Add(new LoyaltyTransaction
        {
            CustomerId = customer.Id,
            OrderId = orderId,
            Type = 0, // Accumulation
            Points = pointsGained,
            Description = $"Accumulated {pointsGained} points for Order ID {orderId}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task<bool> RedeemPointsAsync(int customerId, RedeemPointsRequest request)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer == null) return false;

        if (request.PointsToRedeem <= 0 || request.PointsToRedeem > customer.AvailablePoints)
        {
            throw new InvalidOperationException(_localizer["INSUFFICIENT_LOYALTY_POINTS"] ?? "Số điểm tích lũy không đủ để đổi.");
        }

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId);
        if (order == null) return false;

        // 1 Point = 50 VND
        decimal discountAmount = request.PointsToRedeem * 50;

        customer.AvailablePoints -= request.PointsToRedeem;

        order.DiscountAmount += discountAmount;
        order.TotalAmount = Math.Max(0, order.SubTotal - order.DiscountAmount);

        _context.LoyaltyTransactions.Add(new LoyaltyTransaction
        {
            CustomerId = customer.Id,
            OrderId = order.Id,
            Type = 1, // Redemption
            Points = -request.PointsToRedeem,
            Description = $"Redeemed {request.PointsToRedeem} points for {discountAmount:N0}đ discount on Order ID {order.Id}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task RollbackLoyaltyForCancelledOrderAsync(int orderId)
    {
        var transactions = await _context.LoyaltyTransactions
            .Where(t => t.OrderId == orderId)
            .ToListAsync();

        if (!transactions.Any()) return;

        foreach (var tx in transactions)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == tx.CustomerId);
            if (customer == null) continue;

            if (tx.Type == 0) // Rollback accumulation
            {
                customer.TotalPoints = Math.Max(0, customer.TotalPoints - tx.Points);
                customer.AvailablePoints = Math.Max(0, customer.AvailablePoints - tx.Points);

                _context.LoyaltyTransactions.Add(new LoyaltyTransaction
                {
                    CustomerId = customer.Id,
                    OrderId = orderId,
                    Type = 2, // Reversal
                    Points = -tx.Points,
                    Description = $"Reversed accumulation for cancelled Order ID {orderId}",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else if (tx.Type == 1) // Rollback redemption (Return points)
            {
                var pointsToReturn = Math.Abs(tx.Points);
                customer.AvailablePoints += pointsToReturn;

                _context.LoyaltyTransactions.Add(new LoyaltyTransaction
                {
                    CustomerId = customer.Id,
                    OrderId = orderId,
                    Type = 2, // Reversal
                    Points = pointsToReturn,
                    Description = $"Returned redeemed points for cancelled Order ID {orderId}",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
    }
}
