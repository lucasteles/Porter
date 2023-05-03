namespace Porter;

public interface IProducer<TMessage> where TMessage : notnull
{
    public Task<PublishResult> TryPublish(TMessage message, Guid? correlationId,
        CancellationToken ctx = default);

    public Task<PublishResult> TryPublish(TMessage message, CancellationToken ctx = default) =>
        TryPublish(message, null, ctx);

    public Task Publish(TMessage message, Guid? correlationId, CancellationToken ctx = default);

    public Task Publish(TMessage message, CancellationToken ctx = default) =>
        Publish(message, null, ctx);
}

public interface IProducer<TMessage1, TMessage2>
    where TMessage1 : notnull where TMessage2 : notnull
{
    Task<PublishResult> TryPublish(TMessage1 message, Guid? correlationId,
        CancellationToken ctx = default);

    Task<PublishResult> TryPublish(TMessage1 message, CancellationToken ctx = default) =>
        TryPublish(message, null, ctx);

    Task<PublishResult> TryPublish(TMessage2 message, Guid? correlationId,
        CancellationToken ctx = default);

    Task<PublishResult> TryPublish(TMessage2 message, CancellationToken ctx = default) =>
        TryPublish(message, null, ctx);

    Task Publish(TMessage1 message, Guid? correlationId, CancellationToken ctx = default);

    Task Publish(TMessage1 message, CancellationToken ctx = default) =>
        Publish(message, null, ctx);

    Task Publish(TMessage2 message, Guid? correlationId, CancellationToken ctx = default);

    Task Publish(TMessage2 message, CancellationToken ctx = default) =>
        Publish(message, null, ctx);
}

public interface IProducer<TMessage1, TMessage2, TMessage3>
    where TMessage1 : notnull
    where TMessage2 : notnull
    where TMessage3 : notnull
{
    public Task<PublishResult> TryPublish(TMessage1 message, Guid? correlationId,
        CancellationToken ctx = default);

    public Task<PublishResult> TryPublish(TMessage1 message, CancellationToken ctx = default) =>
        TryPublish(message, null, ctx);

    public Task<PublishResult> TryPublish(TMessage2 message, Guid? correlationId,
        CancellationToken ctx = default);

    public Task<PublishResult> TryPublish(TMessage2 message, CancellationToken ctx = default) =>
        TryPublish(message, null, ctx);

    public Task<PublishResult> TryPublish(TMessage3 message, Guid? correlationId,
        CancellationToken ctx = default);

    public Task<PublishResult> TryPublish(TMessage3 message, CancellationToken ctx = default) =>
        TryPublish(message, null, ctx);

    public Task Publish(TMessage1 message, Guid? correlationId, CancellationToken ctx = default);

    public Task Publish(TMessage1 message, CancellationToken ctx = default) =>
        Publish(message, null, ctx);

    public Task Publish(TMessage2 message, Guid? correlationId, CancellationToken ctx = default);

    public Task Publish(TMessage2 message, CancellationToken ctx = default) =>
        Publish(message, null, ctx);

    public Task Publish(TMessage3 message, Guid? correlationId, CancellationToken ctx = default);

    public Task Publish(TMessage3 message, CancellationToken ctx = default) =>
        Publish(message, null, ctx);
}
