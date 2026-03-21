using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SchoolManagement.Common.Middleware;
using SchoolManagement.Common.Services;
using SchoolManagement.Models.Common;
using Xunit;

namespace SchoolManagement.Tests.Middleware;

public sealed class ExceptionHandlingMiddlewareTests
{
    private static DefaultHttpContext CreateContext(IServiceProvider? serviceProvider = null)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        if (serviceProvider is not null)
            context.RequestServices = serviceProvider;
        return context;
    }

    [Fact]
    public async Task InvokeAsync_ShouldPassThrough_WhenNoException()
    {
        var middleware = new ExceptionHandlingMiddleware(_ => Task.CompletedTask);
        var context = CreateContext(BuildServiceProvider());

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.OK);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn500_OnUnhandledException()
    {
        var middleware = new ExceptionHandlingMiddleware(_ => throw new Exception("Unhandled!"));
        var context = CreateContext(BuildServiceProvider());

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(500);
        var body = await ReadResponseBodyAsync(context);
        body.Should().Contain("INTERNAL_ERROR");
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn404_OnKeyNotFoundException()
    {
        var middleware = new ExceptionHandlingMiddleware(_ => throw new KeyNotFoundException("Not found"));
        var context = CreateContext(BuildServiceProvider());

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn401_OnUnauthorizedAccessException()
    {
        var middleware = new ExceptionHandlingMiddleware(_ => throw new UnauthorizedAccessException());
        var context = CreateContext(BuildServiceProvider());

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn409_OnInvalidOperationException()
    {
        var middleware = new ExceptionHandlingMiddleware(_ => throw new InvalidOperationException("Conflict"));
        var context = CreateContext(BuildServiceProvider());

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn400_OnArgumentException()
    {
        var middleware = new ExceptionHandlingMiddleware(_ => throw new ArgumentException("Bad arg"));
        var context = CreateContext(BuildServiceProvider());

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(400);
    }

    private static IServiceProvider BuildServiceProvider()
    {
        var errorLogMock = new Mock<IErrorLogService>();
        errorLogMock.Setup(s => s.LogAsync(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider.GetService(typeof(IErrorLogService)))
            .Returns(errorLogMock.Object);
        scopeMock.Setup(s => s.ServiceProvider.GetService(typeof(IRequestContext)))
            .Returns(new RequestContext());

        var factoryMock = new Mock<IServiceScopeFactory>();
        factoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var providerMock = new Mock<IServiceProvider>();
        providerMock.Setup(p => p.GetService(typeof(IServiceScopeFactory)))
            .Returns(factoryMock.Object);

        return providerMock.Object;
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await new StreamReader(context.Response.Body).ReadToEndAsync();
    }
}
