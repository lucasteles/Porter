using Navi.Models;

namespace Navi;

public readonly record struct PublishResult(bool IsSuccess, Guid MessageId,
    Guid? CorrelationId);

public sealed class ProduceOptions
{
    public TopicNameOverride? NameOverride { get; set; }
}

public interface IProducerClient
{
    Task<PublishResult> Publish(
        string topicName,
        string message,
        Guid? correlationId = null,
        ProduceOptions? options = null,
        CancellationToken ctx = default);

    Task<PublishResult> Publish<T>(
        string topicName,
        T message,
        Guid? correlationId = null,
        ProduceOptions? options = null,
        CancellationToken ctx = default)
        where T : notnull;
}

public interface IConsumerClient : IDisposable
{
    ValueTask<IReadOnlyCollection<IMessage>> Receive(string topic,
        TopicNameOverride? nameOverride = null,
        CancellationToken ctx = default);

    ValueTask<IReadOnlyCollection<IMessage<T>>> Receive<T>(string topic,
        TopicNameOverride? nameOverride = null,
        CancellationToken ctx = default)
        where T : notnull;

    Task<IReadOnlyCollection<IMessage>> DeadLetters(string queueName,
        TopicNameOverride? nameOverride = null,
        CancellationToken ctx = default);

    Task<IReadOnlyCollection<IMessage<T>>> DeadLetters<T>(string queueName,
        TopicNameOverride? nameOverride = null,
        CancellationToken ctx = default)
        where T : notnull;
}

public interface INaviClient : IProducerClient, IConsumerClient
{
}
