using CrossBorderPayments.Api.Data;
using CrossBorderPayments.Api.DTOs;
using CrossBorderPayments.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrossBorderPayments.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BeneficiariesController : ControllerBase
{
    private readonly AppDbContext _context;

    public BeneficiariesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BeneficiaryDto>>> GetBeneficiaries()
    {
        var beneficiaries = await _context.Beneficiaries
            .AsNoTracking()
            .OrderByDescending(beneficiary => beneficiary.CreatedAtUtc)
            .ToListAsync();

        return Ok(beneficiaries.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BeneficiaryDto>> GetBeneficiary(int id)
    {
        var beneficiary = await _context.Beneficiaries
            .AsNoTracking()
            .FirstOrDefaultAsync(beneficiary => beneficiary.Id == id);

        if (beneficiary is null)
        {
            return NotFound(new
            {
                message = $"Beneficiary with ID {id} was not found."
            });
        }

        return Ok(ToDto(beneficiary));
    }

    [HttpPost]
    public async Task<ActionResult<BeneficiaryDto>> CreateBeneficiary(
        CreateBeneficiaryDto request)
    {
        var beneficiary = new Beneficiary
        {
            Name = request.Name.Trim(),
            Country = request.Country.Trim(),
            Currency = request.Currency.Trim().ToUpperInvariant(),
            BankName = request.BankName.Trim(),
            AccountNumber = request.AccountNumber.Trim()
        };

        _context.Beneficiaries.Add(beneficiary);
        await _context.SaveChangesAsync();

        var response = ToDto(beneficiary);

        return CreatedAtAction(
            nameof(GetBeneficiary),
            new { id = beneficiary.Id },
            response);
    }

    private static BeneficiaryDto ToDto(Beneficiary beneficiary)
    {
        return new BeneficiaryDto
        {
            Id = beneficiary.Id,
            Name = beneficiary.Name,
            Country = beneficiary.Country,
            Currency = beneficiary.Currency,
            BankName = beneficiary.BankName,
            AccountNumberMasked = MaskAccountNumber(
                beneficiary.AccountNumber),
            IsActive = beneficiary.IsActive,
            CreatedAtUtc = beneficiary.CreatedAtUtc
        };
    }

    private static string MaskAccountNumber(string accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            return string.Empty;
        }

        var lastFourCharacters = accountNumber.Length <= 4
            ? accountNumber
            : accountNumber[^4..];

        return $"****{lastFourCharacters}";
    }
}