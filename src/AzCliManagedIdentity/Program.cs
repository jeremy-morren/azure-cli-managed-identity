using AzCliManagedIdentity;
using Serilog;

const string outputTemplate = "[{Timestamp:yy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

try
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console(outputTemplate: outputTemplate)
        .CreateLogger();

    var builder = WebApplication.CreateSlimBuilder(args);

    builder.Host.UseSerilog();

    var app = builder.Build();
    app.UseSerilogRequestLogging();
    new ApiPipeline(new TokenService()).MapEndpoints(app);
    app.Run();
}
catch (Exception e)
{
    Log.Fatal(e, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}