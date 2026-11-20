using BridgeBank.Api.Middleware;
using BridgeBank.Application.Interbank;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Net;

namespace BridgeBank.Api.Tests;

public class InterbankCallerTests
{
    private static InterbankCallerMiddleware NewMiddleware(Action reached)
        => new(
            _ =>
            {
                reached();
                return Task.CompletedTask;
            },
            Options.Create(new InterbankOptions
            {
                PeerCode = "BRDG",
                PeerSecret = "shared-secret",
                AllowedCallers = ["127.0.0.1"]
            }));

    private static DefaultHttpContext Request(string? callerIp, string? secret)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/interbank/credits";
        context.Response.Body = new MemoryStream();

        if (callerIp is not null)
            context.Connection.RemoteIpAddress = IPAddress.Parse(callerIp);

        if (secret is not null)
            context.Request.Headers["X-Bank-Secret"] = secret;

        return context;
    }

    [Theory]
    [InlineData(null, "shared-secret", StatusCodes.Status403Forbidden)]
    [InlineData("10.0.0.9", "shared-secret", StatusCodes.Status403Forbidden)]
    [InlineData("127.0.0.1", null, StatusCodes.Status401Unauthorized)]
    [InlineData("127.0.0.1", "wrong-secret", StatusCodes.Status401Unauthorized)]
    [InlineData("127.0.0.1", "shared-secret", StatusCodes.Status200OK)]
    public async Task Interbank_call_needs_an_allowed_address_and_the_shared_secret(
        string? callerIp, string? secret, int expectedStatus)
    {
        var reached = false;
        var context = Request(callerIp, secret);

        await NewMiddleware(() => reached = true).InvokeAsync(context);

        // 200 means the middleware let it through; nothing else may reach the endpoint.
        if (expectedStatus == StatusCodes.Status200OK)
            Assert.True(reached);
        else
            Assert.False(reached);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
    }

    [Fact]
    public async Task Other_paths_are_not_checked()
    {
        var reached = false;
        var context = Request(null, null);
        context.Request.Path = "/api/account";

        await NewMiddleware(() => reached = true).InvokeAsync(context);

        Assert.True(reached);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }
}
