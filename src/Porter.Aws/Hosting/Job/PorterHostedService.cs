using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Porter.Models;
using Porter.Services;

namespace Porter.Hosting.Job;

class PorterHostedService : BackgroundService
{
    readonly IPorterResourceManager bootstrapper;
    readonly PorterConfig config;
    readonly ImmutableArray<IConsumerDescriber> consumers;
    readonly ImmutableArray<IProducerDescriber> producers;
    readonly IConsumerJob job;
    readonly ILogger<PorterHostedService> logger;
    readonly AsyncServiceScope scope;

    public PorterHostedService(
        IServiceProvider provider,
        IOptions<PorterConfig> config,
        ILogger<PorterHostedService> logger)
    {
        scope = provider.CreateAsyncScope();
        this.logger = logger;
        this.config = config.Value;

        this.bootstrapper = scope.ServiceProvider.GetRequiredService<IPorterResourceManager>();
        this.job = scope.ServiceProvider.GetRequiredService<IConsumerJob>();
        this.consumers = scope.ServiceProvider.GetRequiredService<IEnumerable<IConsumerDescriber>>()
            .ToImmutableArray();
        this.producers = scope.ServiceProvider.GetRequiredService<IEnumerable<IProducerDescriber>>()
            .ToImmutableArray();

        ValidateConsumerConfiguration();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) =>
        await Task.Run(async () =>
            {
                if (await Bootstrap(stoppingToken))
                    await job.Start(consumers, stoppingToken);
            },
            stoppingToken);

    public async Task<bool> Bootstrap(CancellationToken cancellationToken)
    {
        using (logger.BeginScope("Bootstrapping Porter"))
        {
            logger.LogInformation(
                "Naming config: Source={ConfigSource}; Prefix={ConfigPrefix}; Suffix={ConfigSuffix}",
                config.Source, config.Prefix, config.Suffix);

            if (consumers.IsDefaultOrEmpty && producers.IsDefaultOrEmpty)
            {
                logger.LogInformation(
                    "No configured consumers or producers. Skipping configuration");
                return false;
            }

            if (config.Localstack)
                await bootstrapper.SetupLocalstack(cancellationToken);

            logger.LogInformation("Setting up consumers");
            await Task.WhenAll(consumers
                .Select(async d =>
                {
                    logger.LogInformation("Consumer of {ArgTopicName} with {ConsumerTypeName}",
                        d.TopicName, d.ConsumerType.Name);
                    await bootstrapper.EnsureTopicExists(d.TopicName, d.NameOverride,
                        cancellationToken);
                    await bootstrapper.EnsureQueueExists(d.TopicName, d.NameOverride,
                        cancellationToken);

                    await bootstrapper.UpdateQueueAttr(d.TopicName, d.ConsumeTimeout,
                        d.NameOverride,
                        cancellationToken);
                }));

            logger.LogInformation("Setting up producers:");
            await Task.WhenAll(producers
                .Select(async d =>
                {
                    logger.LogInformation("Producer of {ArgTopicName}", d.TopicName);
                    await bootstrapper.EnsureTopicExists(d.TopicName, d.NameOverride,
                        cancellationToken);
                }));

            return true;
        }
    }

    void ValidateConsumerConfiguration()
    {
        var duplicated = consumers
            .GroupBy(d => d.TopicName)
            .Any(g => g.Count() > 1);

        if (duplicated)
            throw new PorterException("Duplicated topic definition");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await scope.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
