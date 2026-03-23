using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ACLS.Domain.Properties;

namespace ACLS.Persistence.Configurations;

public sealed class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.ToTable("Buildings");
        builder.HasKey(b => b.BuildingId);
        builder.Ignore(b => b.Id);

        builder.Property(b => b.BuildingId)
            .ValueGeneratedOnAdd();

        builder.Property(b => b.PropertyId)
            .IsRequired();

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.HasIndex(b => b.PropertyId);

        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(b => b.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
