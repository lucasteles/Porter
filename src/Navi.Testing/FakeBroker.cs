using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Options;
using Navi;
using Navi.Clients;
using Navi.Hosting;
using Navi.Hosting.Job;
using Navi.Models;
using Navi.Services;

namespace Navi.Testing;

public interface IFakeReadonlyBroker
{
    public IReadOnlyDictionary<string, string[]> ProducedMessages();

    public INaviMessageSerializer Serializer { get; }

    string[] GetConsumed(Type consumer, string topic);

    public string[] ProducedOn(string topic);
}

public interface IFakeBroker : IFakeReadonlyBroker
{
    Task<Guid> Produce(string topic, string message, Guid? correlationId = null,
        bool verifyOnly = false);

    Task<Guid> Produce<T>(string topic, T message, Guid? correlationId = null,
        bool verifyOnly = false)
        where T : notnull;

    Task<Guid> Produce<T>(T message, Guid? correlationId = null, bool verifyOnly = false)
        where T : notnull =>
        Produce<T>(typeof(T).Name, message, correlationId, verifyOnly);

    Task<Guid> ProduceVerify<T>(string topic, T message, Guid? correlationId = null)
        where T : notnull =>
        Produce<T>(topic, message, correlationId, verifyOnly: true);

    Task<Guid> ProduceVerify<T>(T message, Guid? correlationId = null)
        where T : notnull =>
        Produce<T>(message, correlationId, verifyOnly: true);

    void Reset();

    T[] ProducedOn<T>(string topic) where T : notnull;

    T[] ProducedOn<T>() where T : notnull =>
        ProducedOn<T>(typeof(T).Name);

    TMessage[] GetConsumed<TConsumer, TMessage>(string topic)
        where TMessage : notnull where TConsumer : IMessageConsumer<TMessage>;

    ConsumedMessage[] GetConsumed(string topic);
    IDictionary<string, ConsumedMessage[]> GetConsumed();

    Task<IReadOnlyDictionary<string, string[]>> Delta(Func<Task> action);
    Task<string[]> Delta(string topic, Func<Task> action);
    Task<T[]> Delta<T>(string topic, Func<Task> action) where T : notnull;

    Task<T[]> Delta<T>(Func<Task> action) where T : notnull =>
        Delta<T>(typeof(T).Name, action);

    void AutoConsumeLoop(bool enabled = true);
    void EnableConsumeLoop() => AutoConsumeLoop(enabled: true);
    void DisableConsumeLoop() => AutoConsumeLoop(enabled: false);
}

public readonly record struct ConsumedMessage(Type Consumer, string Message);

sealed class InMemoryBroker : IConsumeDriver, IProduceDriver, IConsumerJob, INaviResourceManager,
    IFakeBroker
{
    readonly ImmutableDictionary<string, IConsumerDescriber> consumers;

    public INaviMessageSerializer Serializer { get; }

    readonly IConsumerFactory consumerFactory;
    readonly NaviConfig config;
    readonly INaviClock naviClock;

    readonly Dictionary<string, List<IMessage<string>>> produced = new();
    readonly Dictionary<string, List<ConsumedMessage>> consumed = new();
    Dictionary<string, List<IMessage<string>>>? deltaMessages;

    bool autoConsumeLoop = false;

    public InMemoryBroker(
        IConsumerFactory consumerFactory,
        INaviMessageSerializer serializer,
        IEnumerable<IConsumerDescriber> describers,
        IOptions<NaviConfig> config,
        INaviClock naviClock)
    {
        this.consumerFactory = consumerFactory;
        this.Serializer = serializer;
        this.config = config.Value;
        this.naviClock = naviClock;

        this.consumers = describers.ToImmutableDictionary(
            x => x.TopicName, x => x);
    }

    const string OwnHeader = "[TEST_PUBLISH_MESSAGE]";

    public async Task<Guid> Produce<T>(string topic, T message,
        Guid? correlationId = null, bool verifyOnly = false)
        where T : notnull
    {
        var messageBody = Serializer.Serialize(message);
        return await Produce(topic, messageBody, correlationId, verifyOnly);
    }

    public async Task<Guid> Produce(string topic, string message,
        Guid? correlationId = null, bool verifyOnly = false)
    {
        var testMessage = verifyOnly ? message : $"{OwnHeader}{message}";
        var response = await Produce(new TopicId(topic, config), testMessage,
            correlationId, default);
        return response.MessageId;
    }

    public async Task<PublishResult> Produce(TopicId topic, string message,
        Guid? correlationId,
        CancellationToken ctx)
    {
        var owned = message.StartsWith(OwnHeader);
        if (owned)
            message = message[OwnHeader.Length..];

        var topicName = topic.RawName;
        var id = NewId.NextGuid();
        var sentMessage =
            new LocalMessage<string>(message)
            {
                MessageId = id,
                Datetime = naviClock.Now(),
                RetryNumber = 0,
            };

        if (!produced.ContainsKey(topicName))
            produced.Add(topicName, new());
        produced[topicName].Add(sentMessage);

        if (deltaMessages is not null)
        {
            if (!deltaMessages.ContainsKey(topicName))
                deltaMessages.Add(topicName, new());
            deltaMessages[topicName].Add(sentMessage);
        }

        if (consumers.TryGetValue(topicName, out var describer))
        {
            if (autoConsumeLoop || owned)
                await consumerFactory.ConsumeScoped(describer, sentMessage, ctx);

            if (!consumed.TryGetValue(topicName, out var consumedMessages))
                consumed.Add(topicName, new());
            consumed[topicName].Add(new(describer.ConsumerType, message));
        }

        return new PublishResult(true, id, correlationId);
    }

    public void Reset()
    {
        produced.Clear();
        consumed.Clear();
        deltaMessages?.Clear();
        autoConsumeLoop = false;
    }

    static IReadOnlyDictionary<string, string[]> ExtractBody(
        Dictionary<string, List<IMessage<string>>> messages) =>
        messages.ToDictionary(x => x.Key, x => x.Value.Select(v => v.Body).ToArray());

    static string[]
        GetKeyOrEmpty(string key, IReadOnlyDictionary<string, string[]> dict) =>
        dict.TryGetValue(key, out var values) ? values : Array.Empty<string>();

    T[] Deserialize<T>(IEnumerable<string> bodies) where T : notnull =>
        bodies.Select(x => Serializer.Deserialize<T>(x)).ToArray();

    public IReadOnlyDictionary<string, string[]> ProducedMessages() =>
        ExtractBody(produced);

    public string[] ProducedOn(string topic) => GetKeyOrEmpty(topic, ProducedMessages());

    public T[] ProducedOn<T>(string topic) where T : notnull =>
        Deserialize<T>(ProducedOn(topic));

    public ConsumedMessage[] GetConsumed(string topic) =>
        consumed.TryGetValue(topic, out var consumedMessages)
            ? consumedMessages.ToArray()
            : Array.Empty<ConsumedMessage>();

    public IDictionary<string, ConsumedMessage[]> GetConsumed() =>
        consumed.ToImmutableDictionary(x => x.Key, x => x.Value.ToArray());

    public string[] GetConsumed(Type consumer, string topic) =>
        GetConsumed(topic).Where(x => x.Consumer == consumer).Select(x => x.Message)
            .ToArray();

    public TMessage[] GetConsumed<TConsumer, TMessage>(string topic)
        where TConsumer : IMessageConsumer<TMessage> where TMessage : notnull =>
        Deserialize<TMessage>(GetConsumed(typeof(TConsumer), topic));

    public async Task<IReadOnlyDictionary<string, string[]>> Delta(Func<Task> action)
    {
        deltaMessages = new();
        await action();
        var result = ExtractBody(deltaMessages);
        deltaMessages = null;
        return result;
    }

    public async Task<string[]> Delta(string topic, Func<Task> action) =>
        GetKeyOrEmpty(topic, await Delta(action));

    public async Task<T[]> Delta<T>(string topic, Func<Task> action) where T : notnull =>
        Deserialize<T>(await Delta(topic, action));

    public void AutoConsumeLoop(bool enabled = true) => autoConsumeLoop = enabled;

    public Task<IReadOnlyCollection<IMessage<string>>> ReceiveMessages(TopicId topic,
        CancellationToken ctx) =>
        Task.FromResult<IReadOnlyCollection<IMessage<string>>>(
            Array.Empty<IMessage<string>>());

    public Task<IReadOnlyCollection<IMessage<string>>> ReceiveDeadLetters(TopicId topic,
        CancellationToken ctx) =>
        Task.FromResult<IReadOnlyCollection<IMessage<string>>>(
            Array.Empty<IMessage<string>>());

    public Task Start(IReadOnlyCollection<IConsumerDescriber> describers,
        CancellationToken stoppingToken) =>
        Task.CompletedTask;

    public ValueTask EnsureQueueExists(string topic, TopicNameOverride? nameOverride,
        CancellationToken ctx) =>
        ValueTask.CompletedTask;

    public ValueTask EnsureTopicExists(string topic, TopicNameOverride? nameOverride,
        CancellationToken ctx) =>
        ValueTask.CompletedTask;

    public ValueTask EnsureTopicExists(TopicId topic, CancellationToken ctx) =>
        ValueTask.CompletedTask;

    public ValueTask UpdateQueueAttr(string topic, TimeSpan? newTimeout,
        TopicNameOverride? nameOverride,
        CancellationToken ctx) =>
        ValueTask.CompletedTask;

    public Task SetupLocalstack(CancellationToken ctx) => Task.CompletedTask;

    public void Dispose()
    {
        // Method intentionally left empty.
    }
}

class LocalMessage<T> : IMessage<T> where T : notnull
{
    public LocalMessage(T body) => Body = body;

    public Guid MessageId { get; set; }
    public Guid? CorrelationId { get; set; }
    public DateTime Datetime { get; set; }
    public T Body { get; set; }
    public uint RetryNumber { get; set; }
    public string QueueUrl { get; set; } = "";
    public string? TopicArn { get; set; }
    public Task Delete() => Task.CompletedTask;
    public Task Release(TimeSpan delay) => Task.CompletedTask;

    public IMessage<TMap> Map<TMap>(Func<T, TMap> selector) where TMap : notnull =>
        new LocalMessage<TMap>(selector(Body))
        {
            MessageId = MessageId,
            Datetime = Datetime,
            CorrelationId = CorrelationId,
            RetryNumber = RetryNumber,
            TopicArn = TopicArn,
            QueueUrl = QueueUrl,
        };

    public string Location() =>
        $"{TopicArn?.Split(":").LastOrDefault() ?? "???"}.{Path.GetFileName(QueueUrl)}";
}
