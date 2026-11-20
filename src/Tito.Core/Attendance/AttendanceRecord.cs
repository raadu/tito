using Tito.Core.Common;
using Tito.Core.Employees;

namespace Tito.Core.Attendance;

/// <summary>One employee's Time In / Time Out for one company-local day.</summary>
public sealed class AttendanceRecord
{
    public const int CommentMaxLength = 500;

    private AttendanceRecord() { } // for EF Core

    public AttendanceRecord(Employee employee, DateOnly workDate, DateTime timeInUtc, string? comment)
    {
        ArgumentNullException.ThrowIfNull(employee);
        Employee = employee;
        WorkDate = workDate;
        TimeInUtc = timeInUtc;
        TimeInComment = NormalizeComment(comment);
    }

    public int Id { get; private set; }
    public int EmployeeId { get; private set; }               // foreign key column
    public Employee Employee { get; private set; } = null!;   // navigation property (the linked object)
    public DateOnly WorkDate { get; private set; }
    public DateTime TimeInUtc { get; private set; }
    public string? TimeInComment { get; private set; }
    public DateTime? TimeOutUtc { get; private set; }         // `?` on a struct = may have no value
    public string? TimeOutComment { get; private set; }

    /// <summary>Changes on every update; lets the database reject conflicting saves.</summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>Working until timed out, then Done.</summary>
    public AttendanceStatus Status => TimeOutUtc is null ? AttendanceStatus.Working : AttendanceStatus.Done;

    /// <summary>Status for a day that may have no record at all (= Absent).</summary>
    public static AttendanceStatus StatusOf(AttendanceRecord? record) =>
        record?.Status ?? AttendanceStatus.Absent;

    /// <summary>Hours worked: up to Time Out if done, otherwise up to <paramref name="nowUtc"/>.</summary>
    public TimeSpan WorkedDuration(DateTime nowUtc)
    {
        var end = TimeOutUtc ?? nowUtc;
        return end > TimeInUtc ? end - TimeInUtc : TimeSpan.Zero;
    }

    public void TimeOut(DateTime timeOutUtc, string? comment)
    {
        if (TimeOutUtc is not null)
            throw new DomainException(DomainError.Conflict, "You have already timed out today.");
        if (timeOutUtc < TimeInUtc)
            throw new DomainException(DomainError.Validation, "Time Out cannot be earlier than Time In.");

        TimeOutUtc = timeOutUtc;
        TimeOutComment = NormalizeComment(comment);
    }

    private static string? NormalizeComment(string? comment)
    {
        var value = comment?.Trim();
        if (string.IsNullOrEmpty(value))
            return null;
        if (value.Length > CommentMaxLength)
            throw new DomainException(DomainError.Validation, $"Comment must be at most {CommentMaxLength} characters.");
        return value;
    }
}
