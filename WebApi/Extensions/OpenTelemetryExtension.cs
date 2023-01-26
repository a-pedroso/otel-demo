using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using OpenTelemetry;
using System.Reflection;

namespace WebApi.Extensions;

public static class OpenTelemetryExtension
{
    // https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/examples/AspNetCore/Program.cs
    public static IServiceCollection AddOpenTelemetryExtension(this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment webHostEnvironment)
    {
        if (!configuration.GetValue<bool>("OpenTelemetryConfig:Enabled"))
        {
            return services;
        }

        string? otlpExporterUrl = configuration.GetValue<string>("OpenTelemetryConfig:OtlpExporter:AgentUrl");

        ArgumentNullException.ThrowIfNull(otlpExporterUrl);

        services.AddOpenTelemetry()
            .ConfigureResource(resourceBuilder => resourceBuilder.AddService(
                serviceName: webHostEnvironment.ApplicationName,
                serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
                serviceInstanceId: Environment.MachineName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddGrpcClientInstrumentation()
                .AddHttpClientInstrumentation()
                //.AddConsoleExporter()
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(otlpExporterUrl);
                }))
            .WithMetrics(metrics => metrics           
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                //.AddConsoleExporter()
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(otlpExporterUrl);
                }))
            .StartWithHost();

        return services;
    }
}
