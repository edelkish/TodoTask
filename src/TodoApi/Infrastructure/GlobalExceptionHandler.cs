using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TodoApi.Infrastructure;

/// <summary>
/// Traduce excepciones a ProblemDetails: las peticiones mal formadas (JSON inválido, enum desconocido...)
/// dan 400 y cualquier otra excepción no controlada un 500 genérico sin filtrar detalles internos.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem;

        if (exception is BadHttpRequestException)
        {
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Petición no válida",
                Detail = "La petición no es válida (JSON mal formado o valor no reconocido, por ejemplo un estado desconocido)."
            };
        }
        else
        {
            logger.LogError(exception, "Error no controlado en {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Error interno del servidor",
                Detail = "Ocurrió un error inesperado. Inténtalo de nuevo más tarde."
            };
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
