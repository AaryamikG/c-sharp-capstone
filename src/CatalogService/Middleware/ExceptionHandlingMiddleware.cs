using System.Net;
using System.Text.Json;
using CatalogService.Dtos;
using CatalogService.Exceptions;

namespace CatalogService.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (statusCode, error) = MapException(ex);

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception");
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(error, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }

    private static (HttpStatusCode, ApiError) MapException(Exception ex) => ex switch
    {
        BookNotFoundException => (HttpStatusCode.NotFound, new ApiError { Error = "NOT_FOUND", Message = ex.Message }),
        InvalidAvailabilityAdjustmentException => (HttpStatusCode.BadRequest, new ApiError { Error = "VALIDATION_ERROR", Message = ex.Message }),
        _ => (HttpStatusCode.InternalServerError, new ApiError { Error = "INTERNAL_SERVER_ERROR", Message = "An unexpected error occurred" })
    };
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}
