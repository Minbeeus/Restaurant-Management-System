using Restaurant.Application.DTOs;

namespace Restaurant.Application.Interfaces;

public interface IReportService
{
    Task<RevenueReportDto> GetRevenueReportAsync(DateTime? fromDate, DateTime? toDate);
    Task<TopSellingReportDto> GetTopSellingReportAsync(DateTime? fromDate, DateTime? toDate, int limit = 50);
}
