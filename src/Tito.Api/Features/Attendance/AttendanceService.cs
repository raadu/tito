using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Tito.Core.Attendance;
using Tito.Core.Common;
using Tito.Core.Employees;
using Tito.Infrastructure.Persistence;

namespace Tito.Api.Features.Attendance;

/// <summary>Use cases for recording and reading attendance. One instance per HTTP request.</summary>
public sealed class AttendanceService(TitoDbContext db, CompanyClock clock, ILogger<AttendanceService> logger)
{
    /// <summary>Today's state for one employee. Also verifies the name if the code exists.</summary>
    public async Task<AttendanceStatusResponse> GetStatusAsync(string employeeCode, string fullName, CancellationToken ct)
    {
        var code = Employee.NormalizeCode(employeeCode);
        var employee = await FindEmployeeAsync(code, ct);
        if (employee is not null)
            EnsureSameName(employee, fullName);

        var record = employee is null ? null : await FindTodayRecordAsync(employee.Id, ct);
        return ToStatus(code, employee?.FullName, record);
    }

    public async Task<AttendanceStatusResponse> TimeInAsync(AttendanceRequest request, CancellationToken ct)
    {
        EnsureToday(request.Date);
        var now = clock.UtcNow;
        var code = Employee.NormalizeCode(request.EmployeeCode);

        // First visit ever? Create the employee. Otherwise the name must match.
        var employee = await FindEmployeeAsync(code, ct);
        if (employee is null)
        {
            employee = new Employee(code, request.FullName, now);
            db.Employees.Add(employee);
        }
        else
        {
            EnsureSameName(employee, request.FullName);
            if (await FindTodayRecordAsync(employee.Id, ct) is not null)
                throw new DomainException(DomainError.Conflict, "You have already timed in today.");
        }

        var record = new AttendanceRecord(employee, clock.Today, now, request.Comment);
        db.AttendanceRecords.Add(record);

        try
        {
            await db.SaveChangesAsync(ct);   // INSERTs run here, in one transaction
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Two Time In clicks at the same moment: the unique index caught the second one.
            throw new DomainException(DomainError.Conflict, "You have already timed in today.");
        }

        logger.LogInformation("Employee {EmployeeCode} timed in at {TimeInUtc}", code, now);
        return ToStatus(code, employee.FullName, record);
    }

    public async Task<AttendanceStatusResponse> TimeOutAsync(AttendanceRequest request, CancellationToken ct)
    {
        EnsureToday(request.Date);
        var code = Employee.NormalizeCode(request.EmployeeCode);

        var employee = await FindEmployeeAsync(code, ct)
            ?? throw new DomainException(DomainError.Validation, "No Time In found for today. Please time in first.");
        EnsureSameName(employee, request.FullName);

        var record = await FindTodayRecordAsync(employee.Id, ct)
            ?? throw new DomainException(DomainError.Validation, "No Time In found for today. Please time in first.");

        record.TimeOut(clock.UtcNow, request.Comment);   // Core enforces "only once"

        try
        {
            await db.SaveChangesAsync(ct);   // UPDATE ... WHERE Id = @id AND RowVersion = @rv
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone else changed this row since we loaded it (e.g. double click).
            throw new DomainException(DomainError.Conflict, "You have already timed out today.");
        }

        logger.LogInformation("Employee {EmployeeCode} timed out at {TimeOutUtc}", code, record.TimeOutUtc);
        return ToStatus(code, employee.FullName, record);
    }

    /// <summary>All employees with today's status, for the /list board.</summary>
    public async Task<TodayBoardResponse> GetTodayBoardAsync(CancellationToken ct)
    {
        var today = clock.Today;
        var now = clock.UtcNow;

        // One SQL query: every employee + (at most one) record for today.
        var rows = await db.Employees
            .AsNoTracking()                         // read-only: skip change tracking (faster)
            .OrderBy(e => e.FullName)
            .Select(e => new
            {
                e.EmployeeCode,
                e.FullName,
                Record = db.AttendanceRecords
                    .Where(a => a.EmployeeId == e.Id && a.WorkDate == today)
                    .Select(a => new { a.TimeInUtc, a.TimeOutUtc })
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        var employees = rows
            .Select(r =>
            {
                var status = r.Record is null ? AttendanceStatus.Absent
                    : r.Record.TimeOutUtc is null ? AttendanceStatus.Working
                    : AttendanceStatus.Done;
                var worked = r.Record is null ? 0
                    : Minutes((r.Record.TimeOutUtc ?? now) - r.Record.TimeInUtc);
                return new EmployeeTodayResponse(
                    r.EmployeeCode, r.FullName, status, r.Record?.TimeInUtc, r.Record?.TimeOutUtc, worked);
            })
            .ToList();

        return new TodayBoardResponse(today, now, employees);
    }

    // ---------- helpers ----------

    private Task<Employee?> FindEmployeeAsync(string code, CancellationToken ct) =>
        db.Employees.SingleOrDefaultAsync(e => e.EmployeeCode == code, ct);

    private Task<AttendanceRecord?> FindTodayRecordAsync(int employeeId, CancellationToken ct)
    {
        var today = clock.Today;
        return db.AttendanceRecords.SingleOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate == today, ct);
    }

    private void EnsureToday(DateOnly date)
    {
        if (date != clock.Today)
            throw new DomainException(DomainError.Validation, $"Only today's date ({clock.Today:yyyy-MM-dd}) can be selected.");
    }

    private static void EnsureSameName(Employee employee, string fullName)
    {
        if (!employee.HasName(fullName))
            throw new DomainException(DomainError.Conflict,
                $"Employee ID {employee.EmployeeCode} is registered to a different name.");
    }

    private AttendanceStatusResponse ToStatus(string code, string? fullName, AttendanceRecord? record) =>
        new(code,
            fullName,
            AttendanceRecord.StatusOf(record),
            clock.Today,
            record?.TimeInUtc,
            record?.TimeInComment,
            record?.TimeOutUtc,
            record?.TimeOutComment,
            record is null ? 0 : Minutes(record.WorkedDuration(clock.UtcNow)),
            clock.UtcNow);

    private static int Minutes(TimeSpan span) => span <= TimeSpan.Zero ? 0 : (int)span.TotalMinutes;

    // SQL Server error 2601/2627 = duplicate key in a unique index/constraint.
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
