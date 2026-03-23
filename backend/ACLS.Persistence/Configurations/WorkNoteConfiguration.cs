using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ACLS.Domain.Complaints;
using ACLS.Domain.Staff;

namespace ACLS.Persistence.Configurations;

public sealed class WorkNoteConfiguration : IEntityTypeConfiguration<WorkNote>
{
    public void Configure(EntityTypeBuilder<WorkNote> builder)
    {
        builder.ToTable("WorkNotes");
        builder.HasKey(w => w.WorkNoteId);
        builder.Ignore(w => w.Id);

        builder.Property(w => w.WorkNoteId)
            .ValueGeneratedOnAdd();

        builder.Property(w => w.ComplaintId).IsRequired();
        builder.Property(w => w.StaffMemberId).IsRequired();

        builder.Property(w => w.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(w => w.CreatedAt).IsRequired();

        builder.HasIndex(w => w.ComplaintId);

        builder.HasOne<StaffMember>()
            .WithMany()
            .HasForeignKey(w => w.StaffMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
