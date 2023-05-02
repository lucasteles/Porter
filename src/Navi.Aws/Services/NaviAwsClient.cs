using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Navi.Clients;
using Navi.Hosting;
using Navi.Models;

namespace Navi.Services;

sealed class NaviAwsClient : INaviClient
{
    readonly IOptions<NaviConfig> config;
    readonly ILogger<NaviAwsClient> logger;
    readonly INaviResourceManager resources;
    readonly ICorrelationResolver correlationResolver;
    readonly INaviMessageSerializer serializer;

    readonly IProduceDriver producer;
    readonly IConsumeDriver consumer;

    public NaviAwsClient(
        ILogger<NaviAwsClient> logger,
        IOptions<NaviConfig> config,
        INaviMessageSerializer serializer,
        INaviResourceManager resources,
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
        CancellationToken ctx = default)
        where T : notnull
    {
        var message = await Receive(topic, nameOverride, ctx);
        return message.Select(m => m.Map(s => serializer.Deserialize<T>(s))).ToArray();
    }

    public ValueTask<IReadOnlyCollection<IMessage>> Receive(string topic,
        TopicNameOverride? nameOverride = null,
        CancellationToken ctx = default) =>
        Receive(CreateTopicName(topic, nameOverride), ctx);

    internal async ValueTask<IReadOnlyCollection<IMessage>> Receive(TopicId topic,
        CancellationToken ctx) =>
        await consumer.ReceiveMessages(topic, ctx);

    public async Task<IReadOnlyCollection<IMessage<T>>> DeadLetters<T>(
        string queueName,
        TopicNameOverride? nameOverride = null,
        CancellationToken ctx = default)
        where T : notnull
    {
        var message = await DeadLetters(queueName, nameOverride, ctx);
        return message.Select(m => m.Map(s => serializer.Deserialize<T>(s))).ToArray();
    }

    public Task<IReadOnlyCollection<IMessage>> DeadLetters(string queueName,
        TopicNameOverride? nameOverride = null,
        CancellationToken ctx = default)
    {
        var topic = CreateTopicName(queueName, nameOverride);
        return consumer.ReceiveDeadLetters(topic, ctx);
    }

    public Task<PublishResult> Publish<T>(string topicName, T message,
        Guid? correlationId = null,
        ProduceOptions? options = null,
        CancellationToken ctx = default)
        where T : notnull
    {
        var rawMessage = serializer.Serialize(message);
        return Publish(topicName, rawMessage, correlationId, options, ctx);
    }

    public Task<PublishResult> Publish(string topicName, string message,
        Guid? correlationId = null,
        ProduceOptions? options = null,
        CancellationToken ctx = default) =>
        Publish(
            CreateTopicName(topicName, options?.NameOverride),
            message,
            correlationId,
            ctx);

    TopicId CreateTopicName(string name, TopicNameOverride? nameOverride) =>
        new(name, config.Value.FromOverride(nameOverride));

    internal async Task<PublishResult> Publish(
        TopicId topic, string message,
        Guid? correlationId,
        CancellationToken ctx)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(topic);

        logger.LogDebug("{Topic}: Start send message", topic.TopicName);
        await resources.EnsureTopicExists(topic, ctx);
        var validCorrelationId = correlationId ?? correlationResolver.GetId();
        var publishResult =
            await producer.Produce(topic, message, validCorrelationId, ctx);
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
