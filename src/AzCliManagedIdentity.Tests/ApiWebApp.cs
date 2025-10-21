using AzCliManagedIdentity.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Serilog;
using Serilog.Events;

namespace AzCliManagedIdentity.Tests;

public class ApiWebApp : IAsyncDisposable
{
    private readonly WebApplication _app;

    public ApiWebApp(
        int port,
        Action<IEndpointRouteBuilder> configureApp,
        LogEventLevel logLevel = LogEventLevel.Warning)
    {
        var builder = WebApplication.CreateSlimBuilder(["--environment=Development"]);
        builder.WebHost.UseKestrel(o => o.ListenLocalhost(port));

        var logger = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .WriteTo.Console()
            .CreateLogger();
        builder.Host.UseSerilog(logger);

        _app = builder.Build();
        _app.UseAppRequestLogging();
        configureApp(_app);
    }

    public Task StartAsync() => _app.StartAsync();

    public ValueTask DisposeAsync() => _app.DisposeAsync();
}