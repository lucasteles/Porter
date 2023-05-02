using CorrelationId;
using Navi;
using Navi.Hosting;
using NaviPublisher;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOpenTelemetry()
    .WithTracing(b => b
        .AddAspNetCoreInstrumentation()
        .AddSource(NaviTelemetry.SourceName)
        .AddConsoleExporter()
    )
    .WithMetrics(b => b
        .AddAspNetCoreInstrumentation()
        .AddMeter(NaviTelemetry.SourceName)
        .AddConsoleExporter());

builder.Services
    .AddHealthChecks()
    .AddCheck<NaviHealthCheck>("Navi");

builder.Services.AddNavi(nav =>
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
