namespace CrossBorderPayments.Api.Models;

public class Payment
{
    public int Id { get; set; }

    public int BeneficiaryId { get; set; }

    public Beneficiary? Beneficiary { get; set; }

    public decimal SourceAmount { get; set; }

    public required string SourceCurrency { get; set; }

    public decimal DestinationAmount { get; set; }

    public required string DestinationCurrency { get; set; }

    public decimal ExchangeRate { get; set; }

    public decimal Fee { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Draft;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}