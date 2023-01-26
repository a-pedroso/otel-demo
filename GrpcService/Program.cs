using GrpcService.Extensions;
using GrpcService.Services;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddOpenTelemetry(options =>
    {
        options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Init-GrpcService"));
        options.AddConsoleExporter();
    });
});
var logger = loggerFactory.CreateLogger<Program>();

try
{
    logger.LogInformation("App starting");    
    
    var builder = WebApplication.CreateBuilder(args);

    builder.SetupLogging();

    // Add services to the container.
    builder.Services.AddOpenTelemetryExtension(builder.Configuration, builder.Environment);

    builder.Services.AddGrpc();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.MapGrpcService<GreeterService>();
    app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Unhandled exception");
}
finally
{
    logger.LogInformation("Shut down complete");
}