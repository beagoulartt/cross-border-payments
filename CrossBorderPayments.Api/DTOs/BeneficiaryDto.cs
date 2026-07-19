namespace CrossBorderPayments.Api.DTOs;

public class BeneficiaryDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string Currency { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;

    public string AccountNumberMasked { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}