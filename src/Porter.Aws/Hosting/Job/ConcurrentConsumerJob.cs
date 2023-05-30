using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Porter.Models;

namespace Porter.Hosting.Job;

sealed class ConcurrentConsumerJob : IConsumerJob
{
    readonly IOptionsMonitor<PorterConfig> config;
    readonly IConsumerFactory consumerFactory;
    readonly ILogger<ConcurrentConsumerJob> logger;
    readonly IServiceProvider provider;

    public ConcurrentConsumerJob(
        ILogger<ConcurrentConsumerJob> logger,
        IOptionsMonitor<PorterConfig> config,
        IServiceProvider provider,
        IConsumerFactory consumerFactory
    )
    {
        this.logger = logger;
        this.config = config;
        this.provider = provider;
        this.consumerFactory = consumerFactory;
    }

    public async Task Start(
        IReadOnlyCollection<IConsumerDescriber> describers,
        CancellationToken stoppingToken)
    {
        var workers =
            from d in describers
            let channel =
                Channel.CreateBounded<ConsumeRequest>(d.MaxConcurrency)
            from worker in new[]
            {
                PollingWorker(d, channel.Writer, stoppingToken),
                ConsumerWorker(d, channel.Reader, stoppingToken),
            }
            select worker;

        await Task.WhenAll(workers);
    }

    async Task PollingWorker(
        IConsumerDescriber describer,
        ChannelWriter<ConsumeRequest> channel,
        CancellationToken ctx)
    {
        using PeriodicTimer timer = new(describer.PollingInterval);
        await using var scope = provider.CreateAsyncScope();
        using var subs = scope.ServiceProvider.GetRequiredService<IConsumerClient>();

        do
            try
            {
                await channel.WaitToWriteAsync(ctx);
                using var timeoutTokenSource = GetTimeoutTokenSource(ctx);
                var token = timeoutTokenSource.Token;
                logger.LogDebug("{DescriberTopicName}: Polling messages", describer.TopicName);

                var messages = await subs.Receive(describer.TopicName, describer.NameOverride, ctx);

                logger.LogDebug("{DescriberTopicName}: Received {MessagesCount} messages",
                    describer.TopicName, messages.Count);

                var tasks = messages.Select(async m =>
                    await channel.WriteAsync(new(m, token), ctx));

                await Task.WhenAll(tasks);
            }
            catch (TaskCanceledException ex)
            {
                logger.LogWarning(ex,
                    "Subdivisions: Polling Worker Cancelled ({DescriberTopicName})",
                    describer.TopicName);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex,
                    "Porter: Polling Worker Failure ({DescriberTopicName})",
                    describer.TopicName);

                if (config.CurrentValue.RaiseExceptions)
                    throw;
            }
        while (await timer.WaitForNextTickAsync(ctx));

        channel.Complete();
    }

    async Task ConsumerWorker(
        IConsumerDescriber describer,
        ChannelReader<ConsumeRequest> channel,
        CancellationToken stopToken)
    {
        async Task TopicConsumer()
        {
            await foreach (var (message, ctx) in channel.ReadAllAsync(stopToken))
                try
                {
                    ctx.ThrowIfCancellationRequested();
                    await consumerFactory.ConsumeScoped(describer, message, ctx);
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex,
                        "Porter: Consumer Worker Failure ({DescriberTopicName})",
                        describer.TopicName);
                    if (describer.ErrorListener is not null)
                        await describer.ErrorListener(ex);
                    if (config.CurrentValue.RaiseExceptions)
                        throw;
                }
        }

        var tasks = Enumerable
            .Range(0, describer.MaxConcurrency)
            .Select(_ => Task.Run(TopicConsumer, stopToken));
        await Task.WhenAll(tasks);
    }

    CancellationTokenSource GetTimeoutTokenSource(CancellationToken stoppingToken)
    {
        var timeoutToken = new CancellationTokenSource();
        var timeoutSeconds = config.CurrentValue.MessageTimeoutInSeconds;
        timeoutToken.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        var combinedToken =
            CancellationTokenSource.CreateLinkedTokenSource(stoppingToken,
                timeoutToken.Token);
        return combinedToken;
    }

    record struct ConsumeRequest(IMessage<string> Message, CancellationToken Ctx);
}
