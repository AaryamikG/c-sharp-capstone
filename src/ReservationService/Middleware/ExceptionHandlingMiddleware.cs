using System.Net;
using System.Text.Json;
using ReservationService.Dtos;
using ReservationService.Exceptions;

namespace ReservationService.Middleware;

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
            var (statusCode, body) = MapException(ex);

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception");
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(body, body.GetType(), new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }

    private static (HttpStatusCode, object) MapException(Exception ex) => ex switch
    {
        ReservationLimitExceededException e => (HttpStatusCode.BadRequest, new
        {
            error = "RESERVATION_LIMIT_EXCEEDED",
            message = e.Message,
            currentReservations = e.CurrentReservations
        }),
        BookUnavailableException e => (HttpStatusCode.BadRequest, new
        {
            error = "BOOK_UNAVAILABLE",
            message = e.Message,
            availableCopies = e.AvailableCopies
        }),
        InvalidReservationStatusException e => (HttpStatusCode.BadRequest, new
        {
            error = "INVALID_STATUS",
            message = e.Message,
            currentStatus = e.CurrentStatus
        }),
        BookAvailableException => (HttpStatusCode.BadRequest, new ApiError { Error = "BOOK_AVAILABLE", Message = ex.Message }),
        AlreadyWaitlistedException => (HttpStatusCode.BadRequest, new ApiError { Error = "ALREADY_WAITLISTED", Message = ex.Message }),
        ReservationNotFoundException => (HttpStatusCode.NotFound, new ApiError { Error = "NOT_FOUND", Message = ex.Message }),
        WaitlistEntryNotFoundException => (HttpStatusCode.NotFound, new ApiError { Error = "NOT_FOUND", Message = ex.Message }),
        ForbiddenException => (HttpStatusCode.Forbidden, new ApiError { Error = "FORBIDDEN", Message = ex.Message }),
        _ => (HttpStatusCode.InternalServerError, new ApiError { Error = "INTERNAL_SERVER_ERROR", Message = "An unexpected error occurred" })
    };
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}
