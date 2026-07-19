using CrossBorderPayments.Api.Models;

namespace CrossBorderPayments.Api.DTOs;

public class PaymentDto
{
    public int Id { get; set; }

    public int BeneficiaryId { get; set; }

    public string BeneficiaryName { get; set; } = string.Empty;

    public decimal SourceAmount { get; set; }

    public string SourceCurrency { get; set; } = string.Empty;

    public decimal DestinationAmount { get; set; }

    public string DestinationCurrency { get; set; } = string.Empty;

    public decimal ExchangeRate { get; set; }

    public decimal Fee { get; set; }

    public PaymentStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}