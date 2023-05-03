using System.Text.Json.Serialization;
using CorrelationId;
using CorrelationId.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Porter.Models;
using Porter.Services;

namespace Porter.Hosting.Config;

public sealed class PorterConfigBuilder : PorterConfig
{
    readonly IServiceCollection services;
    bool CustomCorrelationId { get; set; }

    public PorterConfigBuilder(IServiceCollection services)
    {
        this.services = services;
        services.AddSingleton(sp =>
            sp.GetService<IEnumerable<ITopicConfigurationBuilder>>()
                ?.Where(x => x.HasConsumer)
                .Select(x => x.CreateConsumerDescriber(sp))
            ?? ArraySegment<IConsumerDescriber>.Empty);

        services.AddSingleton(sp =>
            sp.GetService<IEnumerable<ITopicConfigurationBuilder>>()
                ?.Where(x => !x.HasConsumer)
                .Select(x => x.CreateProducerDescriber())
            ?? ArraySegment<IProducerDescriber>.Empty);
    }

    public TopicConfigurationBuilder<TMessage> MapTopic<TMessage>(string topicName)
        where TMessage : notnull
    {
        var builder = new TopicConfigurationBuilder<TMessage>(services, topicName);
        services.AddSingleton<ITopicConfigurationBuilder>(builder);
        return builder;
    }

    public TopicConfigurationBuilder<TMessage> MapTopic<TMessage>()
        where TMessage : notnull => MapTopic<TMessage>(typeof(TMessage).Name);

    public TopicConfigurationBuilder<TMessage> MapTopic<TMessage>(Delegate handler)
        where TMessage : notnull =>
        MapTopic<TMessage>().WithConsumer(handler);

    public TopicConfigurationBuilder<TMessage> MapTopic<TMessage>(string topicName,
        Delegate handler) where TMessage : notnull =>
        MapTopic<TMessage>(topicName).WithConsumer(handler);

    public TopicConfigurationBuilder<TMessage> MapConsumer<TMessage, TConsumer>()
        where TMessage : notnull
        where TConsumer : class, IConsumer<TMessage> =>
        MapTopic<TMessage>().WithConsumer<TConsumer>();

    public TopicConfigurationBuilder<TMessage> MapConsumer<TMessage, TConsumer>(string topicName)
        where TMessage : notnull
        where TConsumer : class, IConsumer<TMessage> =>
        MapTopic<TMessage>(topicName).WithConsumer<TConsumer>();

    public void RemoveDefaultJsonConverters() => PorterDefaultJsonSerializerConverters.Clear();

    public void AddJsonConverter(params JsonConverter[] converters) =>
        services.AddSingleton<IPorterJsonSerializerConverters>(
            new PorterJsonSerializerConverters(converters));

    public ICorrelationIdBuilder WithCorrelationId(Action<CorrelationIdOptions> config)
    {
        CustomCorrelationId = true;
        return AddPorterCorrelationId(config);
    }

    public void OnError(Action<Exception> handler) => OnError(ex =>
    {
        handler(ex);
        return Task.CompletedTask;
    });

    public void OnError(Func<Exception, Task> handler) =>
        services.AddSingleton<IPorterErrorListener>(new ErrorListener(handler));

    public void OnError<TListener>() where TListener : class, IPorterErrorListener =>
        services.AddSingleton<IPorterErrorListener, TListener>();

    internal void ConfigureOptions(PorterConfig config)
    {
        var defaultConfig = new PorterConfig();
        if (this.Suffix != defaultConfig.Suffix)
            config.Suffix = this.Suffix;
        if (this.Prefix != defaultConfig.Prefix)
            config.Prefix = this.Prefix;
        if (this.Source != defaultConfig.Source)
            config.Source = this.Source;
        if (this.QueueMaxReceiveCount != defaultConfig.QueueMaxReceiveCount)
            config.QueueMaxReceiveCount = this.QueueMaxReceiveCount;
        if (this.RetriesBeforeDeadLetter != defaultConfig.RetriesBeforeDeadLetter)
            config.RetriesBeforeDeadLetter = this.RetriesBeforeDeadLetter;
        if (this.PubKey != defaultConfig.PubKey)
            config.PubKey = this.PubKey;
        if (this.MessageRetentionInDays != defaultConfig.MessageRetentionInDays)
            config.MessageRetentionInDays = this.MessageRetentionInDays;
        if (this.MessageTimeoutInSeconds != defaultConfig.MessageTimeoutInSeconds)
            config.MessageTimeoutInSeconds = this.MessageTimeoutInSeconds;
        if (this.MessageDelayInSeconds != defaultConfig.MessageDelayInSeconds)
            config.MessageDelayInSeconds = this.MessageDelayInSeconds;
        if (Math.Abs(this.PollingIntervalInSeconds -
                     defaultConfig.PollingIntervalInSeconds) > 0)
            config.PollingIntervalInSeconds = this.PollingIntervalInSeconds;
        if (this.ServiceUrl != defaultConfig.ServiceUrl)
            config.ServiceUrl = this.ServiceUrl;
        if (this.Localstack != defaultConfig.Localstack)
            config.Localstack = this.Localstack;
        if (this.AutoCreateNewTopic != defaultConfig.AutoCreateNewTopic)
            config.AutoCreateNewTopic = this.AutoCreateNewTopic;
        if (this.RaiseExceptions != defaultConfig.RaiseExceptions)
            config.RaiseExceptions = this.RaiseExceptions;
        if (this.MapConsumerEndpoints != defaultConfig.MapConsumerEndpoints)
            config.MapConsumerEndpoints = this.MapConsumerEndpoints;
        if (this.Region != defaultConfig.Region)
            config.Region = this.Region;
        if (this.LongPollingWaitInSeconds != defaultConfig.LongPollingWaitInSeconds)
            config.LongPollingWaitInSeconds = this.LongPollingWaitInSeconds;
    }

    ICorrelationIdBuilder AddPorterCorrelationId(
        Action<CorrelationIdOptions>? configure = null)
    {
        var builder = configure is null
            ? services.AddCorrelationId()
            : services.AddCorrelationId(options =>
            {
                options.AddToLoggingScope = true;
                options.UpdateTraceIdentifier = true;
                configure(options);
            });

        builder.WithCustomProvider<NewIdCorrelationIdProvider>();

        return builder;
    }

    internal void ConfigureServices()
    {
        if (!CustomCorrelationId)
            AddPorterCorrelationId();
    }
}
