using GrpcService.Extensions;
using GrpcService.Services;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using StackExchange.Redis;

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

    // https://stackoverflow.com/questions/72621827/redis-cache-calls-in-opentelemetry-in-dotnet
    IConnectionMultiplexer redisConnectionMultiplexer = await ConnectionMultiplexer.ConnectAsync("" + builder.Configuration.GetConnectionString("Redis"));
    builder.Services.AddSingleton(redisConnectionMultiplexer);

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