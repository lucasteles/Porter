namespace Porter;

public interface IProducer<TMessage> where TMessage : notnull
{
    public Task<PublishResult> TryPublish(TMessage message, Guid? correlationId,
        CancellationToken ct = default);

    public Task<PublishResult> TryPublish(TMessage message, CancellationToken ct = default) =>
        TryPublish(message, null, ct);

    public Task Publish(TMessage message, Guid? correlationId, CancellationToken ct = default);

    public Task Publish(TMessage message, CancellationToken ct = default) =>
        Publish(message, null, ct);
}

public interface IProducer<TMessage1, TMessage2>
    where TMessage1 : notnull where TMessage2 : notnull
{
    Task<PublishResult> TryPublish(TMessage1 message, Guid? correlationId,
        CancellationToken ct = default);

    Task<PublishResult> TryPublish(TMessage1 message, CancellationToken ct = default) =>
        TryPublish(message, null, ct);

    Task<PublishResult> TryPublish(TMessage2 message, Guid? correlationId,
        CancellationToken ct = default);

    Task<PublishResult> TryPublish(TMessage2 message, CancellationToken ct = default) =>
        TryPublish(message, null, ct);

    Task Publish(TMessage1 message, Guid? correlationId, CancellationToken ct = default);

    Task Publish(TMessage1 message, CancellationToken ct = default) =>
        Publish(message, null, ct);

    Task Publish(TMessage2 message, Guid? correlationId, CancellationToken ct = default);

    Task Publish(TMessage2 message, CancellationToken ct = default) =>
        Publish(message, null, ct);
}

public interface IProducer<TMessage1, TMessage2, TMessage3>
    where TMessage1 : notnull
    where TMessage2 : notnull
    where TMessage3 : notnull
{
    public Task<PublishResult> TryPublish(TMessage1 message, Guid? correlationId,
        CancellationToken ct = default);

    public Task<PublishResult> TryPublish(TMessage1 message, CancellationToken ct = default) =>
        TryPublish(message, null, ct);

    public Task<PublishResult> TryPublish(TMessage2 message, Guid? correlationId,
        CancellationToken ct = default);

    public Task<PublishResult> TryPublish(TMessage2 message, CancellationToken ct = default) =>
        TryPublish(message, null, ct);

    public Task<PublishResult> TryPublish(TMessage3 message, Guid? correlationId,
        CancellationToken ct = default);

    public Task<PublishResult> TryPublish(TMessage3 message, CancellationToken ct = default) =>
        TryPublish(message, null, ct);

    public Task Publish(TMessage1 message, Guid? correlationId, CancellationToken ct = default);

    public Task Publish(TMessage1 message, CancellationToken ct = default) =>
        Publish(message, null, ct);

    public Task Publish(TMessage2 message, Guid? correlationId, CancellationToken ct = default);

    public Task Publish(TMessage2 message, CancellationToken ct = default) =>
        Publish(message, null, ct);

    public Task Publish(TMessage3 message, Guid? correlationId, CancellationToken ct = default);

    public Task Publish(TMessage3 message, CancellationToken ct = default) =>
        Publish(message, null, ct);
}
