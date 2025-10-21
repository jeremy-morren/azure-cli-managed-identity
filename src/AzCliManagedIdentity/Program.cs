using AzCliManagedIdentity;
using AzCliManagedIdentity.Api;
using AzCliManagedIdentity.ManagedIdentity;
using Serilog;
using Serilog.Events;

const string outputTemplate = "[{Timestamp:yy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

try
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console(outputTemplate: outputTemplate)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
        .CreateLogger();

    SetupAzureCliFiles.CopyFilesOnStartup();

    var builder = WebApplication.CreateSlimBuilder(args);

    builder.Host.UseSerilog();

    var app = builder.Build();

    app.UseAppRequestLogging();

    var tokenService = new TokenService(Log.Logger);
    new ApiPipeline(tokenService).MapEndpoints(app);

    // Map the healthcheck endpoint to run "az login --help"
    app.MapGet("/healthz", new CliHealthcheck("az login --help").Handle);

    app.Run();
    return 0;
}
catch (Exception e)
{
    Log.Fatal(e, "Application failed to start");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}


