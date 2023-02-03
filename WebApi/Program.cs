using Grpc.Net.Client;
using GrpcService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using System.Net.Http;
using System.Text.Json;
using WebApi.Extensions;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddOpenTelemetry(options =>
    {
        options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Init-WebApi"));
        options.AddConsoleExporter();
    });
});
ILogger _logger = loggerFactory.CreateLogger<Program>();

try
{
    _logger.LogInformation("App starting");

    var builder = WebApplication.CreateBuilder(args);

    builder.SetupLogging();

    // Add services to the container.
    builder.Services.AddHttpClient("CatFact", httpClient =>
    {
        httpClient.BaseAddress = new Uri("https://catfact.ninja/");
    });

    builder.Services.AddOpenTelemetryExtension(builder.Configuration, builder.Environment);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    app.MapGet("/weatherforecast", (ILogger<WeatherForecast> loggerWeatherForecast) =>
    {
        loggerWeatherForecast.LogInformation("Get weatherforecast");

        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
            .ToArray();
        return Results.Ok(forecast);
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

    app.MapGet("/catfact", async (ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory) =>
    {
        ILogger loggerCatFact = loggerFactory.CreateLogger("CatFact");
        loggerCatFact.LogInformation("Get Cat Fact");

        var httpClient = httpClientFactory.CreateClient("CatFact");
        var httpResponseMessage = await httpClient.GetAsync("fact");

        if (httpResponseMessage.IsSuccessStatusCode)
        {
            var content = await httpResponseMessage.Content.ReadFromJsonAsync<CatFact>();
            return Results.Ok(content);
        }
        else
        {
            loggerCatFact.LogWarning("failed to retrieve cat fact");
            return Results.Problem(statusCode:503);
        }
    })
    .WithName("GetCatFact")
    .WithOpenApi();


    app.MapGet("/sayhello", async ([FromQuery] string name, IConfiguration config, ILoggerFactory loggerFactory) =>
    {
        ILogger loggerSayHello = loggerFactory.CreateLogger("SayHello");
        loggerSayHello.LogInformation("Get sayhello to {name}", name);

        string? uri = config.GetValue<string>("GrpcService:Uri");
        ArgumentNullException.ThrowIfNull(uri);
        using var channel = GrpcChannel.ForAddress(uri);
        var client = new Greeter.GreeterClient(channel);
        var reply = await client.SayHelloAsync(new HelloRequest { Name = name });
        return Results.Ok(reply.Message);
    })
    .WithName("GetSayHello")
    .WithOpenApi();

    app.Run();
}
catch(Exception ex)
{
    _logger.LogCritical(ex, "Unhandled exception");
}
finally
{
    _logger.LogInformation("Shut down complete");
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

internal record CatFact(string Fact, int Length);
