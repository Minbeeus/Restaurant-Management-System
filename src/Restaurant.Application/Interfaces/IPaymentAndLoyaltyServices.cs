using Restaurant.Application.DTOs;

namespace Restaurant.Application.Interfaces;

public interface ILoyaltyService
{
    Task<CustomerDto?> GetCustomerByPhoneAsync(string phoneNumber);
    Task<List<CustomerDto>> GetAllCustomersAsync();
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request);
    Task AccumulatePointsAsync(int customerId, int orderId, decimal paidAmount);
    Task<bool> RedeemPointsAsync(int customerId, RedeemPointsRequest request);
    Task RollbackLoyaltyForCancelledOrderAsync(int orderId);
}

public interface IPaymentService
{
    Task<VietQrResponse> GenerateVietQrAsync(GenerateVietQrRequest request);
    Task<bool> ProcessSepayWebhookAsync(SepayWebhookPayload payload, string? signatureHeader);
    Task<CashPaymentResponse> ProcessCashPaymentAsync(ProcessCashPaymentRequest request);
}

public interface ICancelOrderService
{
    Task<bool> CancelOrderAsync(int userId, int orderId, string reason);
}
