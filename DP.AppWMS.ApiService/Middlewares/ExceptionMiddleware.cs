using System.ComponentModel.DataAnnotations;
using WMS.Application.Common.Models;
using WMS.Domain.Common;
namespace DP.AppWMS.ApiService.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException appError)
        {
            context.Response.StatusCode = appError.StatusCode;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.Fail(appError.Message);
            response.TraceId = context.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            var (status, msg) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
                DomainException => (StatusCodes.Status400BadRequest, ex.Message),
                ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
                _ => (StatusCodes.Status500InternalServerError, "Internal server error")
            };

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json";

                var response = ApiResponse<object>.Fail(msg);
                response.TraceId = context.TraceIdentifier;

                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
