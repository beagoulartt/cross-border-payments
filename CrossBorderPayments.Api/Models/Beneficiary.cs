namespace CrossBorderPayments.Api.Models;

public class Beneficiary
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Country { get; set; }

    public required string Currency { get; set; }

    public required string BankName { get; set; }

    public required string AccountNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}