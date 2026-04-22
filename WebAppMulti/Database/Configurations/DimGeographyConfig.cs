using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebAppMulti.Database.Models;

namespace WebAppMulti.Database.Configurations
{
    public class DimGeographyConfig : IEntityTypeConfiguration<DimGeography>
    {
        public void Configure(EntityTypeBuilder<DimGeography> builder)
        {
            // Define the primary key
            builder.HasKey(g => g.GeographyKey)
                   .HasName("PK_DimGeography");

            // Optional: Table mapping
            builder.ToTable("DimGeography");

            // Optional: Configure relationships
            builder.HasMany(g => g.DimCustomers)
                   .WithOne(c => c.GeographyKeyNavigation)
                   .HasForeignKey(c => c.GeographyKey);
        }
    }
}
