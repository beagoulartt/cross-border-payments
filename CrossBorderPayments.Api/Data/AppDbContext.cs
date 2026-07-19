using CrossBorderPayments.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrossBorderPayments.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(payment => payment.SourceAmount)
                .HasPrecision(18, 2);

            entity.Property(payment => payment.DestinationAmount)
                .HasPrecision(18, 2);

            entity.Property(payment => payment.Fee)
                .HasPrecision(18, 2);

            entity.Property(payment => payment.ExchangeRate)
                .HasPrecision(18, 6);
        });
    }
}