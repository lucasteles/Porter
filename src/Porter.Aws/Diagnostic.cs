using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Options;
using Porter.Models;
using static Porter.PorterTelemetry;

namespace Porter;

interface IDiagnostics
{
    void SetActivityMessageAttributes(
        Activity? activity,
        string url,
        Guid? messageId,
        Guid? correlationId,
        string message
    );

    Activity? StartProducerActivity(string topic);
    Activity? StartConsumerActivity(string topic);
    Activity? StartProcessActivity(string topic);
    void AddRetrievedMessages(long quantity, string topic);
    void AddConsumedMessagesCounter(long quantity, string topic, TimeSpan duration);
    void AddProducedMessagesCounter(long quantity, string topic);
    void AddFailedMessagesCounter(long quantity, string topic, TimeSpan duration);
    void AddIgnoredMessagesCounter(int quantity, string topic, TimeSpan duration);
    void RecordException(Activity? activity, Exception? ex, string header);
}

public static class PorterTelemetry
{
    public const string SourceName = "Porter";
}

class Diagnostics : IDiagnostics
{
    readonly PorterConfig config;

    static readonly ActivitySource activitySource =
        new(SourceName, Assembly.GetExecutingAssembly().GetName().Version?.ToString());

    static readonly Meter meterSource =
        new(SourceName, Assembly.GetExecutingAssembly().GetName().Version?.ToString());

    static readonly Counter<long> retrievedMessagesCounter =
        meterSource.CreateCounter<long>("retrieved_messages");

    static readonly Counter<long> consumedMessagesCounter =
        meterSource.CreateCounter<long>("consumed_messages");

    static readonly Counter<long> ignoredMessagesCounter =
        meterSource.CreateCounter<long>("ignored_messages");

    static readonly Counter<long> producedMessagesCounter =
        meterSource.CreateCounter<long>("produced_messages");

    static readonly Counter<long> failedMessagesCounter =
        meterSource.CreateCounter<long>("failed_messages");

    public Diagnostics(IOptions<PorterConfig> config) => this.config = config.Value;

    public void SetActivityMessageAttributes(Activity? activity,
        string url,
        Guid? messageId,
        Guid? correlationId,
        string message
    )
    {
        var attr = new Dictionary<string, object>()
        {
            ["messaging.url"] = url,
            ["messaging.message_id"] = messageId ?? Guid.Empty,
            ["messaging.conversation_id"] = correlationId ?? Guid.Empty,
            ["messaging.correlation_id"] = correlationId ?? Guid.Empty,
            ["messaging.message_payload_size_bytes"] =
                Encoding.UTF8.GetByteCount(message),
        };
        foreach (var (key, value) in attr)
            activity?.SetTag(key, value);
    }

    public Activity? StartProducerActivity(string topic) =>
        StartActivity(topic, ActivityKind.Producer, "send");

    public Activity? StartConsumerActivity(string topic)
    {
        const string operation = "receive";
        var activity = StartActivity(topic, ActivityKind.Consumer, operation);
        activity?.SetTag("messaging.operation", operation);
        activity?.SetTag("messaging.consumer_id", config.Source);
        return activity;
    }

    public Activity? StartProcessActivity(string topic)
    {
        const string operation = "process";
        var activity = StartActivity(topic, ActivityKind.Consumer, operation);
        activity?.SetTag("messaging.operation", operation);
        activity?.SetTag("messaging.consumer_id", config.Source);
        return activity;
    }

    static Activity? StartActivity(string topic, ActivityKind kind, string operation)
    {
        var activity = activitySource.StartActivity($"{topic} {operation}", kind);
        activity?.SetTag("messaging.destination_kind", "topic");
        activity?.SetTag("messaging.system", "AmazonSQS");
        activity?.SetTag("messaging.protocol", "AMQP");
        activity?.SetTag("messaging.protocol_version", "");
        activity?.SetTag("messaging.destination", topic);

        return activity;
    }

    static TagList Tags(string topic) => new()
    {
        {
            "topic", topic
        },
    };

    public void AddRetrievedMessages(long quantity, string topic) =>
        retrievedMessagesCounter.Add(quantity, Tags(topic));

    public void AddIgnoredMessagesCounter(int quantity, string topic, TimeSpan duration) =>
        ignoredMessagesCounter.Add(quantity,
            new TagList
            {
                new("duration", duration.TotalMilliseconds),
                new("topic", topic),
            });

    public void AddConsumedMessagesCounter(long quantity, string topic, TimeSpan duration) =>
        consumedMessagesCounter.Add(quantity,
            new TagList
            {
                new("duration", duration.TotalMilliseconds),
                new("topic", topic),
            });

    public void AddProducedMessagesCounter(long quantity, string topic) =>
        producedMessagesCounter.Add(quantity, Tags(topic));

    public void AddFailedMessagesCounter(long quantity, string topic, TimeSpan duration) =>
        failedMessagesCounter.Add(quantity,
            new TagList
            {
                new("duration", duration.TotalMilliseconds),
                new("topic", topic),
            });

    public void RecordException(Activity? activity, Exception? ex, string header)
    {
        activity?.SetStatus(ActivityStatusCode.Error, $"{header}: {ex?.Message}");
        if (activity is null || ex is null)
            return;

        var tagsCollection = new ActivityTagsCollection
        {
            {
                "exception.type", ex.GetType().FullName
            },
            {
                "exception.stacktrace", InvariantString(ex)
            },
        };

        if (!string.IsNullOrWhiteSpace(ex.Message))
            tagsCollection.Add("exception.message", ex.Message);

        activity.AddEvent(new ActivityEvent("exception", default, tagsCollection));
    }

    static string InvariantString(Exception exception)
    {
        var originalUiCulture = Thread.CurrentThread.CurrentUICulture;
        try
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            return exception.ToString();
        }
        finally
        {
            Thread.CurrentThread.CurrentUICulture = originalUiCulture;
        }
    }
}
