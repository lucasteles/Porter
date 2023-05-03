using Porter;
using Porter.Hosting;
using PorterConsumer;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddOpenTelemetry()
    .WithTracing(b => b
        .AddAspNetCoreInstrumentation()
        .AddSource(PorterTelemetry.SourceName)
        .AddConsoleExporter()
    )
    .WithMetrics(b => b
        .AddAspNetCoreInstrumentation()
        .AddMeter(PorterTelemetry.SourceName)
        .AddConsoleExporter());

builder.Services
    .AddHealthChecks()
    .AddCheck<PorterHealthCheck>("Porter");

builder.Services.AddPorter(nav =>
{
    nav.MapConsumer<MyMessage, MyConsumer>("my_topic");
    nav.MapTopic<MyMessage2>("my_other_topic")
        .WithConsumer((MyMessage2 message, ILogger<Program> logger) =>
            logger.LogInformation("Received: {Message}", message.ToString()));
});

var app = builder.Build();

app.MapGet("/", () => "Hello Consumer!");
app.MapHealthChecks("health");
app.Run();
