using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Porter.Models;
using Porter.Services;

namespace Porter.Aws.Tests.TestUtils.Fixtures;

public class PorterClientFixture : LocalstackFixture
{
    protected DateTime fakedDate;
    protected IAmazonSQS sqs = null!;
    private protected TopicId Topic = null!;

    protected string TopicName => Topic.Event;
    protected string QueueName => Topic.QueueName;

    [SetUp]
    public async Task Setup()
    {
        Topic = faker.TopicName(config);
        sqs = GetService<IAmazonSQS>();

        var resources = GetService<IPorterResourceManager>();
        await resources.EnsureQueueExists(TopicName, null, default);

        fakedDate = faker.Date.Soon().ToUniversalTime();
        A.CallTo(() => fakeClock.Now()).Returns(fakedDate);
    }

    protected async Task<IConsumerClient> CreateConsumer(
        Action<PorterConfig>? configure = null) =>
        await NewSubClient(configure, true, false);

    protected async Task<IProducerClient> CreateProducer(
        Action<PorterConfig>? configure = null) =>
        await NewSubClient(configure, false, true);

    protected Task<IPorterClient> CreateSubClient(
        Action<PorterConfig>? configure = null) =>
        NewSubClient(configure, true, true);

    async Task<IPorterClient> NewSubClient(
        Action<PorterConfig>? configure,
        bool isConsumer,
        bool isProducer
    )
    {
        var services = CreatePorterServices(c =>
            {
                ConfigurePorter(c);
                c.Prefix = config.Prefix;
                c.Suffix = config.Suffix;
                c.PubKey = config.PubKey;
                c.Source = $"s{Math.Abs(Guid.NewGuid().GetHashCode())}";
                configure?.Invoke(c);
                c.MessageDelayInSeconds = 0;
            })
            .AddSingleton(fakeClock)
            .AddSingleton<IRetryStrategy, NoRetryStrategy>();
        var provider = services.BuildServiceProvider();
        var resources = provider.GetRequiredService<IPorterResourceManager>();

        if (isProducer)
            await resources.EnsureTopicExists(TopicName, null, default);

        if (isConsumer)
            await resources.EnsureQueueExists(TopicName, null, default);

        return provider.GetRequiredService<IPorterClient>();
    }
}
