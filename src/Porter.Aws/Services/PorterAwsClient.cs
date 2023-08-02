using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Porter.Clients;
using Porter.Hosting;
using Porter.Models;

namespace Porter.Services;

sealed class PorterAwsClient : IPorterClient
{
    readonly IOptions<PorterConfig> config;
    readonly ILogger<PorterAwsClient> logger;
    readonly IPorterResourceManager resources;
    readonly ICorrelationResolver correlationResolver;
    readonly IPorterMessageSerializer serializer;

    readonly IProduceDriver producer;
    readonly IConsumeDriver consumer;

    public PorterAwsClient(
        ILogger<PorterAwsClient> logger,
        IOptions<PorterConfig> config,
        IPorterMessageSerializer serializer,
        IPorterResourceManager resources,
        IProduceDriver producer,
        ICorrelationResolver correlationResolver,
        IConsumeDriver consumer
    )
    {
        this.logger = logger;
        this.config = config;
        this.serializer = serializer;
        this.resources = resources;
        this.correlationResolver = correlationResolver;
        this.producer = producer;
        this.consumer = consumer;
    }

    public async ValueTask<IReadOnlyCollection<IMessage<T>>> Receive<T>(
        string topic,
        TopicNameOverride? nameOverride = null,
        CancellationToken ct = default)
        where T : notnull
    {
        var message = await Receive(topic, nameOverride, ct);
        return message.Select(m => m.Map(s => serializer.Deserialize<T>(s))).ToArray();
    }

    public ValueTask<IReadOnlyCollection<IMessage>> Receive(string topic,
        TopicNameOverride? nameOverride = null,
        CancellationToken ct = default) =>
        Receive(CreateTopicName(topic, nameOverride), ct);

    internal async ValueTask<IReadOnlyCollection<IMessage>> Receive(TopicId topic,
        CancellationToken ct) =>
        await consumer.ReceiveMessages(topic, ct);

    public async Task<IReadOnlyCollection<IMessage<T>>> DeadLetters<T>(
        string queueName,
        TopicNameOverride? nameOverride = null,
        CancellationToken ct = default)
        where T : notnull
    {
        var message = await DeadLetters(queueName, nameOverride, ct);
        return message.Select(m => m.Map(s => serializer.Deserialize<T>(s))).ToArray();
    }

    public Task<IReadOnlyCollection<IMessage>> DeadLetters(string queueName,
        TopicNameOverride? nameOverride = null,
        CancellationToken ct = default)
    {
        var topic = CreateTopicName(queueName, nameOverride);
        return consumer.ReceiveDeadLetters(topic, ct);
    }

    public Task<PublishResult> Publish<T>(string topicName, T message,
        Guid? correlationId = null,
        ProduceOptions? options = null,
        CancellationToken ct = default)
        where T : notnull
    {
        var rawMessage = serializer.Serialize(message);
        return Publish(topicName, rawMessage, correlationId, options, ct);
    }

    public Task<PublishResult> Publish(string topicName, string message,
        Guid? correlationId = null,
        ProduceOptions? options = null,
        CancellationToken ct = default) =>
        Publish(
            CreateTopicName(topicName, options?.NameOverride),
            message,
            correlationId,
            ct);

    TopicId CreateTopicName(string name, TopicNameOverride? nameOverride) =>
        new(name, config.Value.FromOverride(nameOverride));

    internal async Task<PublishResult> Publish(
        TopicId topic, string message,
        Guid? correlationId,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(topic);

        logger.LogDebug("{Topic}: Start send message", topic.TopicName);
        await resources.EnsureTopicExists(topic, ct);
        var validCorrelationId = correlationId ?? correlationResolver.GetId();
        var publishResult =
            await producer.Produce(topic, message, validCorrelationId, ct);
        logger.LogInformation(
            "<- {RawName}[{CorrelationId}.{MessageId}] - Produced {TopicName} - Success: {IsSuccess}",
            topic.RawName, publishResult.CorrelationId, publishResult.MessageId, topic.TopicName,
            publishResult.IsSuccess);
        logger.LogDebug("{Topic}: End send message on", topic.TopicName);
        return publishResult;
    }

    public void Dispose()
    {
        producer.Dispose();
        consumer.Dispose();
    }
}
