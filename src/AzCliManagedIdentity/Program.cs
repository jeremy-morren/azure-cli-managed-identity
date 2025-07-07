using AzCliManagedIdentity;
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
    app.UseSerilogRequestLogging(o => o.GetLevel = GetLevel);
    new ApiPipeline(new TokenService(Log.Logger)).MapEndpoints(app);
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

static LogEventLevel GetLevel(HttpContext context, double elapsed, Exception? ex)
{
    if (ex != null || context.Response.StatusCode >= 500)
        return LogEventLevel.Error;
    if (elapsed > 1000)
        return LogEventLevel.Warning;
    if (context.Request.Path.StartsWithSegments("/healthz"))
        return LogEventLevel.Debug;
    return LogEventLevel.Information;
}