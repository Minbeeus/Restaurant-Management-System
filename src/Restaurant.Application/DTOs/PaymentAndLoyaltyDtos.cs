namespace Restaurant.Application.DTOs;

public class CustomerDto
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int TierId { get; set; }
    public string TierName { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public int AvailablePoints { get; set; }
}

public class CreateCustomerRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class RedeemPointsRequest
{
    public int OrderId { get; set; }
    public int PointsToRedeem { get; set; }
}

public class GenerateVietQrRequest
{
    public int OrderId { get; set; }
    public string BankAccountNo { get; set; } = "000204039999";
    public string BankName { get; set; } = "MBBank";
}

public class VietQrResponse
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string SepayContent { get; set; } = string.Empty;
    public string QrImageUrl { get; set; } = string.Empty;
}

public class SepayWebhookPayload
{
    public long id { get; set; }
    public string gateway { get; set; } = string.Empty;
    public string transactionDate { get; set; } = string.Empty;
    public string accountNumber { get; set; } = string.Empty;
    public string subAccount { get; set; } = string.Empty;
    public string code { get; set; } = string.Empty;
    public string content { get; set; } = string.Empty;
    public string transferType { get; set; } = string.Empty;
    public decimal transferAmount { get; set; }
    public decimal accumulated { get; set; }
    public string referenceCode { get; set; } = string.Empty;
    public string referenceNum { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
}

public class CancelOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}
