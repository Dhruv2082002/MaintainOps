using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MaintainOps.Api.IntegrationTests.ErrorHandling;

/// <summary>
/// The real API, plus the controllers from this test project, which the API doesn't discover on its own.
/// </summary>
public sealed class ErrorTestApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
            services.AddControllers().AddApplicationPart(typeof(ErrorTestController).Assembly));
    }
}
