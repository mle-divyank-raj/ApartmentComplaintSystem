using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ACLS.Domain.Staff;
using ACLS.Persistence.Converters;

namespace ACLS.Persistence.Configurations;

public sealed class StaffMemberConfiguration : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(EntityTypeBuilder<StaffMember> builder)
    {
        builder.ToTable("StaffMembers");
        builder.HasKey(s => s.StaffMemberId);
        builder.Ignore(s => s.Id);

        builder.Property(s => s.StaffMemberId)
            .ValueGeneratedOnAdd();

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.JobTitle)
            .HasMaxLength(100);

        // Skills stored as JSON string: ["Plumbing","Electrical"]
        builder.Property(s => s.Skills)
            .HasConversion(new JsonStringListConverter(), new JsonStringListComparer())
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.Availability)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.AverageRating)
            .HasPrecision(4, 2);

        builder.Property(s => s.LastAssignedAt);

        // UserId must be globally unique — one User can be only one StaffMember
        builder.HasIndex(s => s.UserId)
            .IsUnique();

        // Filtered index: only AVAILABLE staff — dispatch query performance
        builder.HasIndex(s => s.Availability)
            .HasFilter($"[Availability] = '{StaffState.AVAILABLE}'");
    }
}
