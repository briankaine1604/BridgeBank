using BridgeBank.Application.Interbank;
using Microsoft.Extensions.Options;

namespace BridgeBank.Api.Middleware;

public class InterbankCallerMiddleware
{
    private const string SecretHeader = "X-Bank-Secret";

    private readonly RequestDelegate _next;
    private readonly InterbankOptions _options;

    public InterbankCallerMiddleware(RequestDelegate next, IOptions<InterbankOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/interbank"))
        {
            await _next(context);
            return;
        }

        var caller = context.Connection.RemoteIpAddress?.ToString();

        if (caller is null || !_options.AllowedCallers.Contains(caller))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Caller is not allowed.");
            return;
        }

        if (!context.Request.Headers.TryGetValue(SecretHeader, out var secret)
            || secret != _options.PeerSecret)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid bank secret.");
            return;
        }

        await _next(context);
    }
}
