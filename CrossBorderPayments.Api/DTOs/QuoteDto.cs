namespace CrossBorderPayments.Api.DTOs;

public class QuoteDto
{
    public Guid QuoteId { get; set; }

    public decimal SourceAmount { get; set; }

    public string SourceCurrency { get; set; } = string.Empty;

    public decimal ExchangeRate { get; set; }

    public decimal Fee { get; set; }

    public decimal DestinationAmount { get; set; }

    public string DestinationCurrency { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}