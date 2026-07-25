using GymFlow.Application.Common;
using GymFlow.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GymFlow.Api.Infrastructure;

/// <summary>
/// Traduce excepciones conocidas de dominio/aplicación a respuestas HTTP con
/// ProblemDetails, sin filtrar detalles internos.
/// </summary>
public sealed class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var (status, title) = context.Exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflicto"),
            DomainException => (StatusCodes.Status400BadRequest, "Regla de negocio"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Solicitud inválida"),
            _ => (0, string.Empty),
        };

        if (status == 0)
            return; // Excepción no controlada: la maneja el pipeline (500).

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = context.Exception.Message,
        };

        context.Result = new ObjectResult(problem) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
