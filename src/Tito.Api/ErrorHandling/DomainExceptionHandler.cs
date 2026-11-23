using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Tito.Core.Common;

namespace Tito.Api.ErrorHandling;

/// <summary>
/// Turns business-rule violations (DomainException) into ProblemDetails responses.
/// Anything else falls through to the default handler -> 500 without internal details.
/// </summary>
internal sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is not DomainException domain)
            return false;                                  // not ours: let the next handler deal with it

        var status = domain.Error switch                   // switch expression: map enum -> HTTP status
        {
            DomainError.Validation => StatusCodes.Status400BadRequest,
            DomainError.Conflict => StatusCodes.Status409Conflict,
            DomainError.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest,          // `_` = default case
        };

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = domain.Error.ToString(),
                Detail = domain.Message,
            },
        });
    }
}
