using System.ComponentModel.DataAnnotations;

namespace CrossBorderPayments.Api.DTOs;

public class CreatePaymentDto
{
    [Range(1, int.MaxValue)]
    public int BeneficiaryId { get; set; }

    [Required]
    public Guid? QuoteId { get; set; }
}
