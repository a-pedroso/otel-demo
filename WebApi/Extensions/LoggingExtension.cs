using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using System.Reflection;

namespace WebApi.Extensions;

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

        // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-7.0#automatically-log-scope-with-spanid-traceid-parentid-baggage-and-tags

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
