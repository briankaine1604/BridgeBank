using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BridgeBank.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "api-tests-signing-key-that-is-long-enough-for-hmac";
    public const string Issuer = "BridgeBank";
    public const string Audience = "BridgeBankClient";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}
