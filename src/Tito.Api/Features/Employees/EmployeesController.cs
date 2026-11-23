using Microsoft.AspNetCore.Mvc;
using Tito.Api.Features.Attendance;

namespace Tito.Api.Features.Employees;

/// <summary>Employee listings.</summary>
[ApiController]
[Route("api/employees")]
[Produces("application/json")]
public sealed class EmployeesController(AttendanceService service) : ControllerBase
{
    /// <summary>Every known employee with today's status (Absent / Working / Done) for the /list board.</summary>
    /// <param name="ct">Cancelled if the client disconnects.</param>
    [HttpGet("today")]
    [ProducesResponseType<TodayBoardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TodayBoardResponse>> GetToday(CancellationToken ct) =>
        await service.GetTodayBoardAsync(ct);
}
