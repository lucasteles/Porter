using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Porter.Models;

namespace Porter.Hosting.Config;

interface ITopicConfigurationBuilder
{
    bool HasConsumer { get; }
    string TopicName { get; }
    IConsumerDescriber CreateConsumerDescriber(IServiceProvider sp);
    IProducerDescriber CreateProducerDescriber();
}

public sealed class TopicConfigurationBuilder<TMessage> : ITopicConfigurationBuilder
    where TMessage : notnull
{
    readonly IServiceCollection services;
    int? concurrency;
    Type? consumerType;
    Func<Exception, Task>? errorHandler;
    TimeSpan? pollingTime;
    TimeSpan? consumeTimeout;
    TopicNameOverride topicNameOverride = new();

    public string TopicName { get; }

    internal TopicConfigurationBuilder(IServiceCollection services, string topicName)
    {
        this.services = services;
        this.TopicName = topicName;
        services.TryAddScoped<IProducer<TMessage>>(sp => new TypedProducer<TMessage>(
            sp.GetRequiredService<IProducerClient>(),
            topicName,
            new ProduceOptions
            {
                NameOverride = topicNameOverride,
            }
        ));
    }

    public bool HasConsumer => consumerType is not null;

    IConsumerDescriber ITopicConfigurationBuilder.CreateConsumerDescriber(
        IServiceProvider sp)
    {
        var settings = sp.GetRequiredService<IOptions<PorterConfig>>().Value;
        var config =
            new ConsumerConfig
            {
                ErrorHandler = errorHandler,
                NameOverride = topicNameOverride,
                MaxConcurrency = concurrency ?? settings.QueueMaxReceiveCount,
                ConsumeTimeout =
                    consumeTimeout ?? TimeSpan.FromSeconds(settings.MessageTimeoutInSeconds),
                PollingInterval =
                    pollingTime ?? TimeSpan.FromSeconds(settings.PollingIntervalInSeconds),
            };

        if (config.PollingInterval <
            TimeSpan.FromSeconds(settings.LongPollingWaitInSeconds))
            throw new InvalidOperationException(
                $"{nameof(PorterConfig.PollingIntervalInSeconds)} can't be less then {nameof(PorterConfig.LongPollingWaitInSeconds)}");

        return new ConsumerDescriber(
            TopicName,
            consumerType ??
            throw new InvalidOperationException("Consumer type should be specified"),
            typeof(TMessage),
            config);
    }

    IProducerDescriber ITopicConfigurationBuilder.CreateProducerDescriber() =>
        new ProducerDescriber(TopicName, typeof(TMessage), topicNameOverride);

    public TopicConfigurationBuilder<TMessage> WithMaxConcurrency(int max)
    {
        concurrency = max;
        return this;
    }

    public TopicConfigurationBuilder<TMessage> WithTimeout(TimeSpan timeout)
    {
        consumeTimeout = timeout;
        return this;
    }

    public TopicConfigurationBuilder<TMessage> WithPollingInterval(
        TimeSpan interval)
    {
        pollingTime = interval;
        return this;
    }

    public TopicConfigurationBuilder<TMessage> WithoutSuffix() =>
        WithSuffix(string.Empty);

    public TopicConfigurationBuilder<TMessage> WithoutPrefix() =>
        WithPrefix(string.Empty);

    public TopicConfigurationBuilder<TMessage> WithSuffix(
        string suffix)
    {
        topicNameOverride = topicNameOverride with
        {
            Suffix = suffix,
        };
        return this;
    }

    public TopicConfigurationBuilder<TMessage> WithPrefix(
        string prefix)
    {
        topicNameOverride = topicNameOverride with
        {
            Prefix = prefix,
        };
        return this;
    }

    public TopicConfigurationBuilder<TMessage> RawTopicName() =>
        WithoutSuffix().WithoutPrefix();

    public TopicConfigurationBuilder<TMessage> Configure(
        TimeSpan? pollingInterval = null,
        int? maxConcurrency = null)
    {
        concurrency = maxConcurrency;
        pollingTime = pollingInterval;
        return this;
    }

    public TopicConfigurationBuilder<TMessage> WithConsumer<TConsumer>()
        where TConsumer : class, IMessageConsumer<TMessage>
    {
        services.TryAddScoped<TConsumer>();
        consumerType = typeof(TConsumer);
        return this;
    }

    public TopicConfigurationBuilder<TMessage> WithConsumer(Delegate handler)
    {
        DelegateConsumer<TMessage>.ValidateParams(handler);
        services.TryAddScoped(sp => new DelegateConsumer<TMessage>(handler, sp));
        consumerType = typeof(DelegateConsumer<TMessage>);
        return this;
    }

    public TopicConfigurationBuilder<TMessage> OnError(Func<Exception, Task> handler)
    {
        errorHandler = handler;
        return this;
    }
}
