namespace Tito.Core.Attendance;

/// <summary>An employee's state for one day. Colors are the UI mapping on /list.</summary>
public enum AttendanceStatus
{
    Absent,   // black  - no Time In today
    Working,  // green  - timed in, not yet timed out
    Done,     // orange - timed in and timed out
}
