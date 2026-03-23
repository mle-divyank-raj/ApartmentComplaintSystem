using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ACLS.Domain.AuditLog;

namespace ACLS.Persistence.Configurations;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditLog");
        builder.HasKey(a => a.AuditEntryId);
        builder.Ignore(a => a.Id);

        builder.Property(a => a.AuditEntryId).ValueGeneratedOnAdd();

        // Nullable — system-initiated events may not be scoped to a property or user
        builder.Property(a => a.PropertyId);
        builder.Property(a => a.ActorUserId);

        builder.Property(a => a.Action)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityId).IsRequired();

        builder.Property(a => a.ActorRole).HasMaxLength(50);
        builder.Property(a => a.OccurredAt).IsRequired();
        builder.Property(a => a.OldValue).HasMaxLength(4000);
        builder.Property(a => a.NewValue).HasMaxLength(4000);
        builder.Property(a => a.IpAddress).HasMaxLength(45); // IPv6 max

        // Composite index for audit trail by property and time
        builder.HasIndex(a => new { a.PropertyId, a.OccurredAt });
        // Index for audit trail of a specific entity
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
        // Index for actions by a specific user
        builder.HasIndex(a => a.ActorUserId);
    }
}
