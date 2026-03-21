using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Common.Services;
using SchoolManagement.Models.Common;

namespace SchoolManagement.Common.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log error to DB using a new scope so EF context is isolated
        using var scope = context.RequestServices.CreateScope();
        var errorLogService = scope.ServiceProvider.GetService<IErrorLogService>();
        var requestContext = scope.ServiceProvider.GetService<IRequestContext>();

        if (errorLogService is not null)
        {
            await errorLogService.LogAsync(
                exception,
                context.Request.Path,
                context.TraceIdentifier,
                requestContext?.UserId);
        }

        var (statusCode, errorDetail) = exception switch
        {
            ArgumentException ex => (HttpStatusCode.BadRequest, ErrorDetail.Create("BAD_REQUEST", ex.Message)),
            UnauthorizedAccessException ex => (HttpStatusCode.Unauthorized, ErrorDetail.Unauthorized(ex.Message)),
            KeyNotFoundException ex => (HttpStatusCode.NotFound, ErrorDetail.NotFound(ex.Message)),
            InvalidOperationException ex => (HttpStatusCode.Conflict, ErrorDetail.Conflict(ex.Message)),
            _ => (HttpStatusCode.InternalServerError, ErrorDetail.Internal())
        };

        var response = ApiResponse<object>.Fail(errorDetail, context.TraceIdentifier);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
