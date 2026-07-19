namespace CrossBorderPayments.Api.Models;

public enum PaymentStatus
{
    Draft,
    Submitted,
    PendingApproval,
    Processing,
    Completed,
    Rejected,
    Failed
}