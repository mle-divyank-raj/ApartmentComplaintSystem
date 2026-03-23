using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ACLS.Domain.Outages;

namespace ACLS.Persistence.Configurations;

public sealed class OutageConfiguration : IEntityTypeConfiguration<Outage>
{
    public void Configure(EntityTypeBuilder<Outage> builder)
    {
        builder.ToTable("Outages");
        builder.HasKey(o => o.OutageId);
        builder.Ignore(o => o.Id);

        builder.Property(o => o.OutageId).ValueGeneratedOnAdd();
        builder.Property(o => o.PropertyId).IsRequired();
        builder.Property(o => o.DeclaredByManagerUserId).IsRequired();

        builder.Property(o => o.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.OutageType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(o => o.StartTime).IsRequired();
        builder.Property(o => o.EndTime);
        builder.Property(o => o.DeclaredAt).IsRequired();
        builder.Property(o => o.NotificationSentAt);

        builder.HasIndex(o => o.PropertyId);
        builder.HasIndex(o => new { o.PropertyId, o.StartTime });

        builder.HasOne<Domain.Identity.User>()
            .WithMany()
            .HasForeignKey(o => o.DeclaredByManagerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
