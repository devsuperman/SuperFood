namespace SuperFood.Domain.Enums;

/// <summary>
/// Unified order lifecycle. Delivery orders (US-0804) use the full range;
/// dine-in orders (US-0701..0706) typically stop at Completed/Cancelled.
/// </summary>
public enum OrderStatus
{
    Received,
    Accepted,
    Preparing,
    Ready,
    OutForDelivery,
    Completed,
    Cancelled,
    Rejected
}
