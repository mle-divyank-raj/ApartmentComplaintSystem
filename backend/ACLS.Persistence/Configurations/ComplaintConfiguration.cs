using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ACLS.Domain.Complaints;
using ACLS.Domain.Staff;
using ACLS.Persistence.Converters;

namespace ACLS.Persistence.Configurations;

public sealed class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> builder)
    {
        builder.ToTable("Complaints");
        builder.HasKey(c => c.ComplaintId);
        builder.Ignore(c => c.Id);

        builder.Property(c => c.ComplaintId)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.PropertyId).IsRequired();
        builder.Property(c => c.UnitId).IsRequired();
        builder.Property(c => c.ResidentId).IsRequired();
        builder.Property(c => c.AssignedStaffMemberId);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(ComplaintConstants.MaxTitleLength);

        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(ComplaintConstants.MaxDescriptionLength);

        builder.Property(c => c.Category)
            .IsRequired()
            .HasMaxLength(ComplaintConstants.MaxCategoryLength);

        // RequiredSkills stored as JSON string
        builder.Property(c => c.RequiredSkills)
            .HasConversion(new JsonStringListConverter(), new JsonStringListComparer())
            .HasMaxLength(ComplaintConstants.MaxRequiredSkillsJsonLength)
            .IsRequired();

        builder.Property(c => c.Urgency)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.PermissionToEnter).IsRequired();
        builder.Property(c => c.Eta);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();
        builder.Property(c => c.ResolvedAt);
        builder.Property(c => c.Tat).HasPrecision(10, 2);
        builder.Property(c => c.ResidentRating);

        builder.Property(c => c.ResidentFeedbackComment)
            .HasMaxLength(ComplaintConstants.MaxFeedbackCommentLength);

        builder.Property(c => c.FeedbackSubmittedAt);

        // Navigation collections (private backing fields)
        builder.HasMany(c => c.Media).WithOne()
            .HasForeignKey(m => m.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.WorkNotes).WithOne()
            .HasForeignKey(w => w.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        // Nullable FK to StaffMember
        builder.HasOne<StaffMember>()
            .WithMany()
            .HasForeignKey(c => c.AssignedStaffMemberId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Indexes (from ERD Index Summary) ─────────────────────────────
        builder.HasIndex(c => c.PropertyId);
        builder.HasIndex(c => new { c.PropertyId, c.Status });
        builder.HasIndex(c => new { c.PropertyId, c.AssignedStaffMemberId });
        builder.HasIndex(c => new { c.PropertyId, c.UnitId });
        builder.HasIndex(c => new { c.PropertyId, c.ResidentId });
        builder.HasIndex(c => c.CreatedAt);

        // Private collection backing fields EF Core navigation configuration
        builder.Navigation(c => c.Media)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(c => c.WorkNotes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
