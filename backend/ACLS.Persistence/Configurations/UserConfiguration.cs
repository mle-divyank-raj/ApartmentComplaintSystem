using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ACLS.Domain.Identity;
using ACLS.Domain.Residents;
using ACLS.Domain.Staff;

namespace ACLS.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.UserId);
        builder.Ignore(u => u.Id);

        builder.Property(u => u.UserId)
            .ValueGeneratedOnAdd();

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(254);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.Phone)
            .HasMaxLength(20);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(u => u.PropertyId)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        // Email is globally unique across the entire platform
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasIndex(u => u.PropertyId);
        builder.HasIndex(u => u.Role);

        // FK to Properties — scoping
        builder.HasOne<Domain.Properties.Property>()
            .WithMany()
            .HasForeignKey(u => u.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1:1 extension tables — Resident and StaffMember
        builder.HasOne<Resident>()
            .WithOne()
            .HasForeignKey<Resident>(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StaffMember>()
            .WithOne()
            .HasForeignKey<StaffMember>(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore computed property
        builder.Ignore(u => u.FullName);
    }
}
