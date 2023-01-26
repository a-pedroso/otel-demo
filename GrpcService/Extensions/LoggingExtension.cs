using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using System.Reflection;

namespace GrpcService.Extensions;

public static class LoggingExtension
{
    public static void SetupLogging(this WebApplicationBuilder builder)
    {
        if (!builder.Configuration.GetValue<bool>("OpenTelemetryConfig:Enabled"))
        {
            return;
        }

        string? otlpExporterUrl = builder.Configuration.GetValue<string>("OpenTelemetryConfig:OtlpExporter:AgentUrl");

        ArgumentNullException.ThrowIfNull(otlpExporterUrl);

        builder.Logging.ClearProviders();

        builder.Logging
            .Configure(options =>
            {
                options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId
                                                | ActivityTrackingOptions.TraceId
                                                | ActivityTrackingOptions.ParentId
                                                | ActivityTrackingOptions.Baggage
                                                | ActivityTrackingOptions.Tags;
            })
            .AddOpenTelemetry(logging =>
            {
                logging.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(
                    serviceName: builder.Environment.ApplicationName,
                    serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
                    serviceInstanceId: Environment.MachineName));
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
                logging.ParseStateValues = true;
                logging.AddConsoleExporter();
                logging.AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(otlpExporterUrl);
                });
            });
    }
}
