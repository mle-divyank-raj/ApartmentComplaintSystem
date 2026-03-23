using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ACLS.Domain.Identity;

namespace ACLS.Persistence.Configurations;

public sealed class InvitationTokenConfiguration : IEntityTypeConfiguration<InvitationToken>
{
    public void Configure(EntityTypeBuilder<InvitationToken> builder)
    {
        builder.ToTable("InvitationTokens");
        builder.HasKey(t => t.InvitationTokenId);
        builder.Ignore(t => t.Id);

        builder.Property(t => t.InvitationTokenId)
            .ValueGeneratedOnAdd();

        builder.Property(t => t.UnitId).IsRequired();
        builder.Property(t => t.PropertyId).IsRequired();

        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.IssuedByManagerUserId).IsRequired();
        builder.Property(t => t.UsedByUserId);
        builder.Property(t => t.IssuedAt).IsRequired();
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.UsedAt);
        builder.Property(t => t.IsRevoked).IsRequired();

        // Token string is globally unique
        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => t.UnitId);
        builder.HasIndex(t => t.PropertyId);

        // Two distinct FKs to Users — explicit constraint names required to prevent shadowing
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.IssuedByManagerUserId)
            .HasConstraintName("FK_InvitationTokens_IssuedByManager")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UsedByUserId)
            .HasConstraintName("FK_InvitationTokens_UsedByResident")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
