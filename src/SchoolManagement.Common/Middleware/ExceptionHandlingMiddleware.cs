using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Common.Services;
using SchoolManagement.Models.Common;

namespace SchoolManagement.Common.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Enable buffering BEFORE _next so the request body becomes seekable.
        // This allows us to re-read the body in the catch block even after the
        // controller/binding layer has already consumed it.
        context.Request.EnableBuffering();

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
        // ── 1. Read request body (body is buffered — seek back to start) ─────
        string? requestPayload = null;
        if (context.Request.Body.CanSeek)
        {
            context.Request.Body.Position = 0;
            using var reader = new StreamReader(
                context.Request.Body, Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 4096, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            requestPayload = string.IsNullOrWhiteSpace(body) ? null : body;
        }

        // ── 2. Determine status code and build the error response JSON ────────
        var (statusCode, errorDetail) = exception switch
        {
            ArgumentException ex          => (HttpStatusCode.BadRequest,         ErrorDetail.Create("BAD_REQUEST", ex.Message)),
            UnauthorizedAccessException ex => (HttpStatusCode.Unauthorized,       ErrorDetail.Unauthorized(ex.Message)),
            KeyNotFoundException ex        => (HttpStatusCode.NotFound,           ErrorDetail.NotFound(ex.Message)),
            InvalidOperationException ex   => (HttpStatusCode.Conflict,           ErrorDetail.Conflict(ex.Message)),
            _                             => (HttpStatusCode.InternalServerError, ErrorDetail.Internal())
        };

        var response = ApiResponse<object>.Fail(errorDetail, context.TraceIdentifier);
        var json = JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // ── 3. Log to DB ──────────────────────────────────────────────────────
        // IRequestContext → original request scope (already has Location/UserId populated
        //                   by RequestContextMiddleware earlier in the pipeline).
        // IErrorLogService → new isolated scope (EF DbContext must not share a
        //                    potentially-corrupted scope that caused the exception).
        var requestContext = context.RequestServices.GetService<IRequestContext>();

        using var scope = context.RequestServices.CreateScope();
        var errorLogService = scope.ServiceProvider.GetService<IErrorLogService>();

        if (errorLogService is not null)
        {
            await errorLogService.LogAsync(new ErrorLogEntry(
                Exception:       exception,
                RequestPath:     context.Request.Path,
                TraceId:         context.TraceIdentifier,
                UserId:          requestContext?.UserId,
                IpAddress:       context.Connection.RemoteIpAddress?.ToString(),
                Location:        requestContext?.Location,
                HttpMethod:      context.Request.Method,
                StatusCode:      (int)statusCode,
                RequestPayload:  requestPayload,
                ResponsePayload: json
            ));
        }

        // ── 4. Write the error response ───────────────────────────────────────
        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)statusCode;
        await context.Response.WriteAsync(json);
    }
}
