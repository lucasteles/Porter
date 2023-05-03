using CorrelationId;
using Porter;
using Porter.Hosting;
using PorterPublisher;
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
    nav.MapTopic<MyMessage>("my_topic");
});

var app = builder.Build();
app.UseCorrelationId();

app.MapGet("/publish/{name}", async (
    IProducer<MyMessage> publisher,
    ILogger<Program> logger,
    string name
) =>
{
    logger.LogInformation("Publishing message");
    var message = new MyMessage
    {
        Id = Guid.NewGuid(),
        Name = name,
        BirthDate = DateTime.Now,
        Age = Random.Shared.Next(),
    };
    await publisher.TryPublish(message);
});

app.MapGet("/", () => "Hello Publisher!");
app.MapHealthChecks("health");

app.Run();
