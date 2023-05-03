namespace Porter;

public interface IMessageMeta
{
    Guid MessageId { get; init; }
    string Queue { get; init; }
    string MessageBody { get; init; }
    DateTime DateTime { get; init; }
    Guid? CorrelationId { get; init; }
    string? Topic { get; init; }
}

sealed class MessageMeta : IMessageMeta
{
    public required Guid MessageId { get; init; }
    public required string Queue { get; init; }
    public required string MessageBody { get; init; }
    public required DateTime DateTime { get; init; }
    public Guid? CorrelationId { get; init; }
    public string? Topic { get; init; }

    public static MessageMeta FromMessage(IMessage message) => new()
    {
        MessageId = message.MessageId,
        DateTime = message.Datetime,
        Queue = message.QueueUrl,
        Topic = message.TopicArn,
        MessageBody = message.Body,
        CorrelationId = message.CorrelationId,
    };
}

public interface IWeakConsumer
{
    internal Task Consume(object message, IMessageMeta meta, CancellationToken ctx);
}

public interface IMessageConsumer<in TMessage> : IWeakConsumer where TMessage : notnull
{
    Task IWeakConsumer.Consume(object message, IMessageMeta meta, CancellationToken ctx) =>
        Consume((TMessage)message, meta, ctx);

    Task Consume(TMessage message, IMessageMeta meta, CancellationToken ctx);
}

public interface IConsumer<in TMessage> : IMessageConsumer<TMessage> where TMessage : notnull
{
    Task IMessageConsumer<TMessage>.
        Consume(TMessage message, IMessageMeta meta, CancellationToken ctx) =>
        Consume(message, ctx);

    Task Consume(TMessage message, CancellationToken ctx);
}

public interface IMessageConsumer : IMessageConsumer<string>
{
}
