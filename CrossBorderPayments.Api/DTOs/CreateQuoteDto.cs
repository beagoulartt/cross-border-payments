using System.ComponentModel.DataAnnotations;

namespace CrossBorderPayments.Api.DTOs;

public class CreateQuoteDto
{
    public decimal SourceAmount { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string SourceCurrency { get; set; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string DestinationCurrency { get; set; } = string.Empty;
}