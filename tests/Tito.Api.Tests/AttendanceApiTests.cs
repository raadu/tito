using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Tito.Api.Features.Attendance;
using Tito.Core.Attendance;

namespace Tito.Api.Tests;

public sealed class AttendanceApiTests(TitoApiFactory factory) : IClassFixture<TitoApiFactory>
{
    private const string Name = "Abdul Basit";

    // Same JSON settings as the API: camelCase + enums as strings.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client = factory.CreateClient();

    // A fresh employee code per test, e.g. "T3F9A1C2B".
    private static string NewCode() => "T" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    private static AttendanceRequest Request(string code, string name = Name, DateOnly? date = null, string? comment = null) =>
        new(code, name, date ?? TitoApiFactory.Today, comment);

    private Task<HttpResponseMessage> GetStatus(string code, string name = Name) =>
        _client.GetAsync($"/api/attendance/status?employeeCode={code}&fullName={Uri.EscapeDataString(name)}");

    [Fact]
    public async Task Full_day_flow_absent_working_done()
    {
        var code = NewCode();

        // 1. Unknown employee -> Absent
        var before = await (await GetStatus(code)).Content.ReadFromJsonAsync<AttendanceStatusResponse>(Json);
        Assert.Equal(AttendanceStatus.Absent, before!.Status);

        // 2. Time In -> Working
        var timeIn = await _client.PostAsJsonAsync("/api/attendance/time-in", Request(code, comment: "Office"), Json);
        Assert.Equal(HttpStatusCode.OK, timeIn.StatusCode);
        var working = await timeIn.Content.ReadFromJsonAsync<AttendanceStatusResponse>(Json);
        Assert.Equal(AttendanceStatus.Working, working!.Status);
        Assert.Equal("Office", working.TimeInComment);
        Assert.Equal(DateTimeKind.Utc, working.TimeInUtc!.Value.Kind);   // JSON ended with "Z"

        // 3. 2h15m later the status shows minutes worked so far
        factory.Clock.Advance(TimeSpan.FromMinutes(135));
        var later = await (await GetStatus(code)).Content.ReadFromJsonAsync<AttendanceStatusResponse>(Json);
        Assert.Equal(135, later!.WorkedMinutes);

        // 4. Time Out -> Done, total frozen
        var timeOut = await _client.PostAsJsonAsync("/api/attendance/time-out", Request(code), Json);
        Assert.Equal(HttpStatusCode.OK, timeOut.StatusCode);
        var done = await timeOut.Content.ReadFromJsonAsync<AttendanceStatusResponse>(Json);
        Assert.Equal(AttendanceStatus.Done, done!.Status);
        Assert.Equal(135, done.WorkedMinutes);

        // 5. The /list board shows the employee as Done
        var board = await _client.GetFromJsonAsync<TodayBoardResponse>("/api/employees/today", Json);
        var row = Assert.Single(board!.Employees, e => e.EmployeeCode == code);
        Assert.Equal(AttendanceStatus.Done, row.Status);
    }

    [Fact]
    public async Task Second_time_in_same_day_is_409()
    {
        var code = NewCode();
        await _client.PostAsJsonAsync("/api/attendance/time-in", Request(code), Json);

        var second = await _client.PostAsJsonAsync("/api/attendance/time-in", Request(code), Json);

        await AssertProblem(second, HttpStatusCode.Conflict, "already timed in");
    }

    [Fact]
    public async Task Time_out_without_time_in_is_400()
    {
        var response = await _client.PostAsJsonAsync("/api/attendance/time-out", Request(NewCode()), Json);

        await AssertProblem(response, HttpStatusCode.BadRequest, "No Time In");
    }

    [Fact]
    public async Task Date_other_than_today_is_400()
    {
        var yesterday = TitoApiFactory.Today.AddDays(-1);

        var response = await _client.PostAsJsonAsync("/api/attendance/time-in", Request(NewCode(), date: yesterday), Json);

        await AssertProblem(response, HttpStatusCode.BadRequest, "Only today's date");
    }

    [Fact]
    public async Task Existing_code_with_different_name_is_409()
    {
        var code = NewCode();
        await _client.PostAsJsonAsync("/api/attendance/time-in", Request(code), Json);

        var response = await GetStatus(code, "Someone Else");

        await AssertProblem(response, HttpStatusCode.Conflict, "different name");
    }

    [Fact]
    public async Task Missing_employee_code_is_400_validation_error()
    {
        var response = await _client.PostAsJsonAsync("/api/attendance/time-in", Request(""), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        Assert.Contains("EmployeeCode", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Time_in_rejects_time_out_fields_in_payload()
    {
        var code = NewCode();
        var body = new
        {
            employeeCode = code,
            fullName = Name,
            date = TitoApiFactory.Today,
            timeOutUtc = "2026-11-20T20:00:00Z",   // not part of AttendanceRequest
            timeOutComment = "should be rejected",
        };

        var response = await _client.PostAsJsonAsync("/api/attendance/time-in", body, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // Nothing was saved: the employee is still absent.
        var status = await (await GetStatus(code)).Content.ReadFromJsonAsync<AttendanceStatusResponse>(Json);
        Assert.Equal(AttendanceStatus.Absent, status!.Status);
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode expected, string detailContains)
    {
        Assert.Equal(expected, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);
        Assert.Contains(detailContains, problem!.Detail);
    }
}
