using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tito.Core.Attendance;

namespace Tito.Infrastructure.Persistence.Configurations;

internal sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords");
        builder.HasKey(a => a.Id);

        // Relationship: many records -> one employee, via EmployeeId (FOREIGN KEY).
        // Restrict = deleting an employee with records is blocked (no silent data loss).
        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.WorkDate).IsRequired();    // DateOnly -> SQL `date`
        builder.Property(a => a.TimeInUtc).IsRequired();   // DateTime -> SQL `datetime2`
        builder.Property(a => a.TimeInComment).HasMaxLength(AttendanceRecord.CommentMaxLength);
        builder.Property(a => a.TimeOutComment).HasMaxLength(AttendanceRecord.CommentMaxLength);

        // SQL Server `rowversion`: auto-changes on every UPDATE; EF uses it for optimistic concurrency.
        builder.Property(a => a.RowVersion).IsRowVersion();

        // Business rule "one session per employee per day", enforced by the database itself.
        builder.HasIndex(a => new { a.EmployeeId, a.WorkDate }).IsUnique();
        // Speeds up "everyone's status for date X" (/list) and date-range stats.
        builder.HasIndex(a => a.WorkDate);

        // `Status` is computed in C#, not stored.
        builder.Ignore(a => a.Status);
    }
}
