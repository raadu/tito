using System.ComponentModel.DataAnnotations;
using Tito.Core.Attendance;
using Tito.Core.Employees;

namespace Tito.Api.Features.Attendance;

// ---------- Requests (JSON the client sends) ----------

/// <summary>Body for both POST /time-in and POST /time-out.</summary>
public sealed record AttendanceRequest(
    [Required, StringLength(Employee.CodeMaxLength)] string EmployeeCode,
    [Required, StringLength(Employee.NameMaxLength)] string FullName,
    DateOnly Date,
    [StringLength(AttendanceRecord.CommentMaxLength)] string? Comment);

// ---------- Responses (JSON the API returns) ----------

/// <summary>One employee's state for today. Drives the Time In / Time Out button.</summary>
public sealed record AttendanceStatusResponse(
    string EmployeeCode,
    string? FullName,             // null when the code has never been used
    AttendanceStatus Status,      // "Absent" | "Working" | "Done"
    DateOnly Today,               // company-local date
    DateTime? TimeInUtc,
    string? TimeInComment,
    DateTime? TimeOutUtc,
    string? TimeOutComment,
    int WorkedMinutes,            // 0 if absent; live while working; final once done
    DateTime ServerNowUtc);       // lets the browser correct a wrong device clock

/// <summary>One row on the /list status board.</summary>
public sealed record EmployeeTodayResponse(
    string EmployeeCode,
    string FullName,
    AttendanceStatus Status,
    DateTime? TimeInUtc,
    DateTime? TimeOutUtc,
    int WorkedMinutes);

/// <summary>The whole /list board, with server time for live counters.</summary>
public sealed record TodayBoardResponse(
    DateOnly Today,
    DateTime ServerNowUtc,
    IReadOnlyList<EmployeeTodayResponse> Employees);
