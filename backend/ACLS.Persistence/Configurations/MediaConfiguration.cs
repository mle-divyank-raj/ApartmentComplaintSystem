using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ACLS.Domain.Complaints;

namespace ACLS.Persistence.Configurations;

public sealed class MediaConfiguration : IEntityTypeConfiguration<Media>
{
    public void Configure(EntityTypeBuilder<Media> builder)
    {
        builder.ToTable("Media");
        builder.HasKey(m => m.MediaId);
        builder.Ignore(m => m.Id);

        builder.Property(m => m.MediaId)
            .ValueGeneratedOnAdd();

        builder.Property(m => m.ComplaintId).IsRequired();

        builder.Property(m => m.Url)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(m => m.Type)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.UploadedByUserId).IsRequired();
        builder.Property(m => m.UploadedAt).IsRequired();

        builder.HasIndex(m => m.ComplaintId);

        builder.HasOne<Domain.Identity.User>()
            .WithMany()
            .HasForeignKey(m => m.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
