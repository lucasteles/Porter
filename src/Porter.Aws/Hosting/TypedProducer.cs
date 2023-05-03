using Porter.Models;

namespace Porter.Hosting;

public sealed class PorterPublishException : PorterException
{
    internal PorterPublishException(Guid id, Guid? correlation) : base(
        $"Unable to publish message {correlation?.ToString() ?? "no-correlation"}.{id}")
    {
    }
}

sealed class TypedProducer<TMessage> : IProducer<TMessage> where TMessage : notnull
{
    readonly IProducerClient producer;
    readonly string topicName;
    readonly ProduceOptions? options;

    public TypedProducer(IProducerClient producer, string topicName,
        ProduceOptions? options)
    {
        this.topicName = topicName;
        this.options = options;
        this.producer = producer;
    }

    public Task<PublishResult> TryPublish(TMessage message, Guid? correlationId,
        CancellationToken ctx = default) =>
        producer.Publish(topicName, message, correlationId, options, ctx);

    public async Task Publish(TMessage message, Guid? correlationId,
        CancellationToken ctx = default)
    {
        var result = await TryPublish(message, correlationId, ctx);

        if (!result.IsSuccess)
            throw new PorterPublishException(
                result.MessageId,
                result.CorrelationId
            );
    }
}

sealed class TypedProducer<TMessage1, TMessage2> : IProducer<TMessage1, TMessage2>
    where TMessage1 : notnull where TMessage2 : notnull
{
    readonly IProducer<TMessage1> producer1;
    readonly IProducer<TMessage2> producer2;

    public TypedProducer(IProducer<TMessage1> producer1, IProducer<TMessage2> producer2)
    {
        this.producer1 = producer1;
        this.producer2 = producer2;
    }

    public Task<PublishResult>
        TryPublish(TMessage1 message, Guid? correlationId,
            CancellationToken ctx = default) =>
        producer1.TryPublish(message, correlationId, ctx);

    public Task<PublishResult>
        TryPublish(TMessage2 message, Guid? correlationId,
            CancellationToken ctx = default) =>
        producer2.TryPublish(message, correlationId, ctx);

    public Task
        Publish(TMessage1 message, Guid? correlationId,
            CancellationToken ctx = default) =>
        producer1.Publish(message, correlationId, ctx);

    public Task
        Publish(TMessage2 message, Guid? correlationId,
            CancellationToken ctx = default) =>
        producer2.Publish(message, correlationId, ctx);
}

sealed class TypedProducer<TMessage1, TMessage2, TMessage3> :
    IProducer<TMessage1, TMessage2, TMessage3>
    where TMessage1 : notnull where TMessage2 : notnull where TMessage3 : notnull
{
    readonly IProducer<TMessage1> producer1;
    readonly IProducer<TMessage2> producer2;
    readonly IProducer<TMessage3> producer3;

    public TypedProducer(IProducer<TMessage1> producer1, IProducer<TMessage2> producer2,
        IProducer<TMessage3> producer3)
    {
        this.producer1 = producer1;
        this.producer2 = producer2;
        this.producer3 = producer3;
    }

    public Task<PublishResult>
        TryPublish(TMessage1 message, Guid? correlationId,
            CancellationToken ctx = default) =>
        producer1.TryPublish(message, correlationId, ctx);

    public Task<PublishResult>
        TryPublish(TMessage2 message, Guid? correlationId,
            CancellationToken ctx = default) =>
        producer2.TryPublish(message, correlationId, ctx);

    public Task<PublishResult>
        TryPublish(TMessage3 message, Guid? correlationId,
            CancellationToken ctx = default) =>
        producer3.TryPublish(message, correlationId, ctx);

    public Task
        Publish(TMessage1 message, Guid? correlationId,
            CancellationToken ctx = default) =>
        producer1.TryPublish(message, correlationId, ctx);

    public Task
        Publish(TMessage2 message, Guid? correlationId,
            CancellationToken ctx = default) =>
        producer2.TryPublish(message, correlationId, ctx);

    public Task
        Publish(TMessage3 message, Guid? correlationId,
            CancellationToken ctx = default) =>
        producer3.TryPublish(message, correlationId, ctx);
}
