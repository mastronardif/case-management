using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAppMulti.Database.Models;

namespace WebAppMulti.Database.Configurations
{
    public class EmployeeConfig : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.ToTable("DimEmployee");

            builder.HasKey(e => e.EmployeeKey);

            builder.Property(e => e.DepartmentName)
                .HasMaxLength(100)  // Assuming max length; adjust if you know actual max length
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(e => e.BaseRate)
                .HasColumnType("decimal(18, 2)");

            builder.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsRequired(false);

            builder.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsRequired(false);

            builder.Property(e => e.HireDate)
                .HasColumnType("date");

            builder.Property(e => e.BirthDate)
                .HasColumnType("date");

            builder.Property(e => e.Title)
                .HasMaxLength(50)
                .IsRequired(false);

            builder.Property(e => e.EmailAddress)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsRequired(false);

            builder.Property(e => e.SalariedFlag)
                .IsRequired();

            builder.Property(e => e.PayFrequency)
                .IsRequired();

            builder.Property(e => e.VacationHours)
                .IsRequired();

            builder.Property(e => e.SickLeaveHours)
                .IsRequired();

            builder.Property(e => e.Status)
                .HasMaxLength(50)
                .IsRequired(false);
        }
    }
}
