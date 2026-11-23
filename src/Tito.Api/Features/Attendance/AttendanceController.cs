using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Tito.Api.Features.Attendance;

/// <summary>Time In / Time Out for employees.</summary>
[ApiController]                       // auto 400 on invalid input, [FromBody] inferred, ProblemDetails errors
[Route("api/attendance")]
[Produces("application/json")]
public sealed class AttendanceController(AttendanceService service) : ControllerBase
{
    /// <summary>Today's status for an employee: decides whether the button says Time In or Time Out.</summary>
    /// <param name="employeeCode">The employee ID typed on the home screen.</param>
    /// <param name="fullName">The full name typed on the home screen.</param>
    /// <param name="ct">Cancelled if the client disconnects.</param>
    [HttpGet("status")]
    [ProducesResponseType<AttendanceStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AttendanceStatusResponse>> GetStatus(
        [FromQuery] string employeeCode, [FromQuery] string fullName, CancellationToken ct) =>
        await service.GetStatusAsync(employeeCode, fullName, ct);

    /// <summary>Records Time In for today.</summary>
    [HttpPost("time-in")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    [ProducesResponseType<AttendanceStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AttendanceStatusResponse>> TimeIn(AttendanceRequest request, CancellationToken ct) =>
        await service.TimeInAsync(request, ct);

    /// <summary>Records Time Out for today. Requires a Time In first.</summary>
    [HttpPost("time-out")]
    [EnableRateLimiting(RateLimitPolicies.Writes)]
    [ProducesResponseType<AttendanceStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AttendanceStatusResponse>> TimeOut(AttendanceRequest request, CancellationToken ct) =>
        await service.TimeOutAsync(request, ct);
}

/// <summary>Names of rate-limit policies (defined in Program.cs).</summary>
public static class RateLimitPolicies
{
    public const string Writes = "writes";
}
