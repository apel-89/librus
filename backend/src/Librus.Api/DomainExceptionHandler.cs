using Librus.Api.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Librus.Api;

public sealed class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not DomainException domainException)
            return false;

        var (status, title) = domainException switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Hittades inte"),
            ConflictException => (StatusCodes.Status409Conflict, "Åtgärden kunde inte utföras"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Åtkomst nekad"),
            _ => (StatusCodes.Status400BadRequest, "Ogiltig begäran"),
        };

        logger.LogInformation(
            "Domänfel {Status} på {Path}: {Message}",
            status, context.Request.Path, domainException.Message);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = domainException.Message,
            Instance = $"{context.Request.Method} {context.Request.Path}",
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}