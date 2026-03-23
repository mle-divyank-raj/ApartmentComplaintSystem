using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ACLS.Domain.Residents;

namespace ACLS.Persistence.Configurations;

public sealed class ResidentConfiguration : IEntityTypeConfiguration<Resident>
{
    public void Configure(EntityTypeBuilder<Resident> builder)
    {
        builder.ToTable("Residents");
        builder.HasKey(r => r.ResidentId);
        builder.Ignore(r => r.Id);

        builder.Property(r => r.ResidentId)
            .ValueGeneratedOnAdd();

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.Property(r => r.UnitId)
            .IsRequired();

        builder.Property(r => r.LeaseStart);
        builder.Property(r => r.LeaseEnd);

        // UserId must be globally unique — one User can only be one Resident
        builder.HasIndex(r => r.UserId)
            .IsUnique();

        builder.HasIndex(r => r.UnitId);

        builder.HasOne<Domain.Properties.Unit>()
            .WithMany()
            .HasForeignKey(r => r.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
