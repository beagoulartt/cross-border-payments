using System.ComponentModel.DataAnnotations;

namespace CrossBorderPayments.Api.DTOs;

public class CreatePaymentDto
{
    [Range(1, int.MaxValue)]
    public int BeneficiaryId { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "9999999999999999")]
    public decimal SourceAmount { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string SourceCurrency { get; set; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string DestinationCurrency { get; set; } = string.Empty;
}