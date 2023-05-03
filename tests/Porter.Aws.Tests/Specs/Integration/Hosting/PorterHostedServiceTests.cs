using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Porter.Aws.Tests.Builders;
using Porter.Aws.Tests.TestUtils;
using Porter.Aws.Tests.TestUtils.Fixtures;
using Porter.Extensions;
using Porter.Hosting;
using Porter.Hosting.Job;
using Porter.Models;

namespace Porter.Aws.Tests.Specs.Integration.Hosting;

public class PorterHostedServiceTests : LocalstackFixture
{
    ConsumerDescriber[] fakeConsumers = null!;
    ConsumerDescriber updated = null!;
    ProducerDescriber[] fakeProducers = null!;

    protected override void ConfigurePorter(PorterConfig c)
    {
        base.ConfigurePorter(c);
        c.Prefix = "";
        c.Source = "x";
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        updated = new ConsumerDescriber(
            faker.TopicNameString(),
            typeof(FakeMessageConsumer),
            typeof(string),
            new ConsumerConfig
            {
                ConsumeTimeout = TimeSpan.FromSeconds(faker.Random.Int(1, 220)),
            }
        );

        var ns = Enumerable.Range(0, faker.Random.Int(3, 10)).ToArray();
        fakeConsumers =
            ns.Select(_ => new ConsumerDescriber(
                    faker.TopicNameString(),
                    typeof(FakeMessageConsumer),
                    typeof(string)))
                .Append(updated)
                .Concat(
                    ns.Select(_ =>
                        new ConsumerDescriber(faker.TopicNameString(), typeof(FakeConsumer),
                            typeof(string))))
                .ToArray();

        fakeProducers = Enumerable
            .Range(0, faker.Random.Int(1, 5))
            .Select(_ => new ProducerDescriber(faker.TopicNameString(), typeof(string), null))
            .ToArray();

        services
            .AddSingleton<IConsumerFactory, ConsumerFactory>()
            .AddSingleton<IConsumerJob, ConcurrentConsumerJob>()
            .AddHostedService<PorterHostedService>();

        foreach (var consumer in fakeConsumers)
            services
                .AddSingleton<IConsumerDescriber>(consumer);

        foreach (var producer in fakeProducers)
            services
                .AddSingleton<IProducerDescriber>(producer);
    }

    [Test]
    public async Task ShouldCreateAllRules()
    {
        var hosted = (PorterHostedService)GetService<IHostedService>();

        await hosted.Bootstrap(default);

        var topics =
            fakeConsumers.Select(x => x.TopicName.ToPascalCase())
                .Concat(
                    fakeProducers.Select(x => x.TopicName.ToPascalCase()));

        var ev = GetService<IAmazonEventBridge>();
        var savedRules = await ev.ListRulesAsync(new ListRulesRequest());
        var rules = savedRules.Rules.Select(x => x.Name);

        rules.Should().BeEquivalentTo(topics);
    }

    [Test]
    public async Task ShouldCreateAllTopics()
    {
        var hosted = (PorterHostedService)GetService<IHostedService>();
        await hosted.Bootstrap(default);

        var topics =
            fakeConsumers.Select(x => x.TopicName.ToPascalCase())
                .Concat(
                    fakeProducers.Select(x => x.TopicName.ToPascalCase()));

        var sns = GetService<IAmazonSimpleNotificationService>();
        var savedRules = await sns.ListTopicsAsync();
        var snsTopics = savedRules.Topics.Select(x => x.TopicArn.Split(":").Last());

        snsTopics.Should().BeEquivalentTo(topics);
    }

    [Test]
    public async Task ShouldCreateConsumerQueues()
    {
        var hosted = (PorterHostedService)GetService<IHostedService>();
        await hosted.Bootstrap(default);

        var normalQueues = fakeConsumers.Select(x => $"x_{x.TopicName}").ToArray();
        var expectedQs = normalQueues.Concat(normalQueues.Select(x => $"dead_letter_{x}"));

        var sqs = GetService<IAmazonSQS>();
        var queueUrls = await sqs.ListQueuesAsync(new ListQueuesRequest());
        var queues = queueUrls.QueueUrls.Select(Path.GetFileName);

        queues.Should().BeEquivalentTo(expectedQs);
    }

    [Test]
    public async Task ShouldNotCreateProducerQueues()
    {
        var hosted = (PorterHostedService)GetService<IHostedService>();
        await hosted.Bootstrap(default);

        var queueNames = fakeProducers.Select(x => $"x_{x.TopicName}").ToArray();

        var sqs = GetService<IAmazonSQS>();
        var queueUrls = await sqs.ListQueuesAsync(new ListQueuesRequest());
        var queues = queueUrls.QueueUrls.Select(Path.GetFileName);

        queues.Should().NotContain(queueNames);
    }

    [Test]
    public async Task ShouldOverrideTimeout()
    {
        var hosted = (PorterHostedService)GetService<IHostedService>();
        await hosted.Bootstrap(default);

        var sqs = GetService<IAmazonSQS>();
        var queueUrls = await sqs.ListQueuesAsync(new ListQueuesRequest());
        var queue = queueUrls.QueueUrls
            .Select(Path.GetFileName)
            .Single(x => x == $"x_{updated.TopicName}")!;

        var attr = await sqs.GetQueueInfo(queue);
        attr.VisibilityTimeout.Should().Be((int)updated.ConsumeTimeout.TotalSeconds);
    }
}
