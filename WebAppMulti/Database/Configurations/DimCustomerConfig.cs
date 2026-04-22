using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAppMulti.Database.Models;

namespace WebAppMulti.Database.Configurations;

public class DimCustomerConfig : IEntityTypeConfiguration<DimCustomer>
{
    public void Configure(EntityTypeBuilder<DimCustomer> builder)
    {
        builder.HasKey(e => e.CustomerKey).HasName("PK_DimCustomer_CustomerKey");

        builder.ToTable("DimCustomer");

        builder.HasIndex(e => e.CustomerAlternateKey, "IX_DimCustomer_CustomerAlternateKey").IsUnique();

        builder.Property(e => e.AddressLine1).HasMaxLength(120);
        builder.Property(e => e.AddressLine2).HasMaxLength(120);
        builder.Property(e => e.CommuteDistance).HasMaxLength(15);
        builder.Property(e => e.CustomerAlternateKey).HasMaxLength(15);
        builder.Property(e => e.EmailAddress).HasMaxLength(50);
        builder.Property(e => e.EnglishEducation).HasMaxLength(40);
        builder.Property(e => e.EnglishOccupation).HasMaxLength(100);
        builder.Property(e => e.FirstName).HasMaxLength(50);
        builder.Property(e => e.FrenchEducation).HasMaxLength(40);
        builder.Property(e => e.FrenchOccupation).HasMaxLength(100);
        builder.Property(e => e.Gender).HasMaxLength(1);
        builder.Property(e => e.HouseOwnerFlag).HasMaxLength(1).IsFixedLength();
        builder.Property(e => e.LastName).HasMaxLength(50);
        builder.Property(e => e.MaritalStatus).HasMaxLength(1).IsFixedLength();
        builder.Property(e => e.MiddleName).HasMaxLength(50);
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.SpanishEducation).HasMaxLength(40);
        builder.Property(e => e.SpanishOccupation).HasMaxLength(100);
        builder.Property(e => e.Suffix).HasMaxLength(10);
        builder.Property(e => e.Title).HasMaxLength(8);
        builder.Property(e => e.YearlyIncome).HasColumnType("money");
    }
}
