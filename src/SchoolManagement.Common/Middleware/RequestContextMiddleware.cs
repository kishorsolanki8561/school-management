using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SchoolManagement.Common.Services;
using SchoolManagement.Common.Utilities;

namespace SchoolManagement.Common.Middleware;

public sealed class RequestContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IIpLocationResolver _locationResolver;

    public RequestContextMiddleware(RequestDelegate next, IIpLocationResolver locationResolver)
    {
        _next = next;
        _locationResolver = locationResolver;
    }

    public async Task InvokeAsync(HttpContext context, IRequestContext requestContext)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();
        requestContext.IpAddress = ip;
        requestContext.Location = await _locationResolver.ResolveAsync(ip);
        requestContext.ScreenName = context.Request.Headers["X-Screen-Name"].FirstOrDefault();
        requestContext.TraceId = context.TraceIdentifier;

        if (context.User.Identity?.IsAuthenticated == true)
        {
            requestContext.UserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            requestContext.Username = context.User.FindFirstValue(ClaimTypes.Name);
            requestContext.Role = context.User.FindFirstValue(ClaimTypes.Role);
        }

        await _next(context);
    }
}
