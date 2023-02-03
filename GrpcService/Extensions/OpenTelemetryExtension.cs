using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry;
using System.Reflection;
using OpenTelemetry.Metrics;

namespace GrpcService.Extensions;

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
            .ConfigureResource(r => r.AddService(
                serviceName: webHostEnvironment.ApplicationName,
                serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
                serviceInstanceId: Environment.MachineName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddRedisInstrumentation()
                //.AddConsoleExporter()
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(otlpExporterUrl);
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                //.AddConsoleExporter()
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(otlpExporterUrl);
                }))
            .StartWithHost();

        return services;
    }
}
