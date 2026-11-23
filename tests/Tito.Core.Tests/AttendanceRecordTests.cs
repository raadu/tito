using Tito.Core.Attendance;
using Tito.Core.Common;
using Tito.Core.Employees;

namespace Tito.Core.Tests;

public class AttendanceRecordTests
{
    private static readonly DateTime TimeIn = new(2026, 11, 20, 3, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 11, 20);

    private static AttendanceRecord NewRecord() =>
        new(new Employee("E-01", "Abdul Basit", TimeIn), Today, TimeIn, "  on site  ");

    [Fact]
    public void No_record_means_absent() =>
        Assert.Equal(AttendanceStatus.Absent, AttendanceRecord.StatusOf(null));

    [Fact]
    public void New_record_is_working_with_trimmed_comment()
    {
        var record = NewRecord();

        Assert.Equal(AttendanceStatus.Working, record.Status);
        Assert.Equal("on site", record.TimeInComment);
    }

    [Fact]
    public void WorkedDuration_counts_up_to_now_while_working()
    {
        var record = NewRecord();
        var now = TimeIn.AddHours(2).AddMinutes(15);

        Assert.Equal(new TimeSpan(2, 15, 0), record.WorkedDuration(now));
    }

    [Fact]
    public void TimeOut_marks_done_and_freezes_duration()
    {
        var record = NewRecord();

        record.TimeOut(TimeIn.AddHours(8), comment: null);

        Assert.Equal(AttendanceStatus.Done, record.Status);
        // Asking later still returns 8h: the clock stopped at Time Out.
        Assert.Equal(TimeSpan.FromHours(8), record.WorkedDuration(TimeIn.AddHours(20)));
    }

    [Fact]
    public void Second_TimeOut_is_a_conflict()
    {
        var record = NewRecord();
        record.TimeOut(TimeIn.AddHours(8), null);

        var ex = Assert.Throws<DomainException>(() => record.TimeOut(TimeIn.AddHours(9), null));

        Assert.Equal(DomainError.Conflict, ex.Error);
    }

    [Fact]
    public void TimeOut_before_TimeIn_is_rejected()
    {
        var record = NewRecord();

        var ex = Assert.Throws<DomainException>(() => record.TimeOut(TimeIn.AddMinutes(-1), null));

        Assert.Equal(DomainError.Validation, ex.Error);
    }
}
