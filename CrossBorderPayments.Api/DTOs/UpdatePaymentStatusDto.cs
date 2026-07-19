using CrossBorderPayments.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace CrossBorderPayments.Api.DTOs;

public class UpdatePaymentStatusDto
{
    [Required]
    public PaymentStatus? Status { get; set; }
}