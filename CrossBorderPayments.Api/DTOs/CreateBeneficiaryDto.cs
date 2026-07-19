using System.ComponentModel.DataAnnotations;

namespace CrossBorderPayments.Api.DTOs;

public class CreateBeneficiaryDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Country { get; set; } = string.Empty;

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string BankName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string AccountNumber { get; set; } = string.Empty;
}