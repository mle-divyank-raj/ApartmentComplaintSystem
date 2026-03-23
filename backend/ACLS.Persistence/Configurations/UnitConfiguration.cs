using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ACLS.Domain.Properties;

namespace ACLS.Persistence.Configurations;

public sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ToTable("Units");
        builder.HasKey(u => u.UnitId);
        builder.Ignore(u => u.Id);

        builder.Property(u => u.UnitId)
            .ValueGeneratedOnAdd();

        builder.Property(u => u.BuildingId)
            .IsRequired();

        builder.Property(u => u.UnitNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(u => u.Floor)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.HasIndex(u => u.BuildingId);

        // Unique: no duplicate unit numbers within the same building
        builder.HasIndex(u => new { u.BuildingId, u.UnitNumber })
            .IsUnique();

        builder.HasOne<Building>()
            .WithMany()
            .HasForeignKey(u => u.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
