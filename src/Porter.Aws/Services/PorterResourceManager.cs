using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Porter.Clients;
using Porter.Extensions;
using Porter.Models;

namespace Porter.Services;

interface IPorterResourceManager
{
    ValueTask EnsureQueueExists(string topic, TopicNameOverride? nameOverride,
        CancellationToken ct);

    ValueTask EnsureTopicExists(string topic, TopicNameOverride? nameOverride,
        CancellationToken ct);

    ValueTask EnsureTopicExists(TopicId topic, CancellationToken ct);

    ValueTask UpdateQueueAttr(string topic, TimeSpan? newTimeout, TopicNameOverride? nameOverride,
        CancellationToken ct);

    Task SetupLocalstack(CancellationToken ct);
}

class AwsResourceManager : IPorterResourceManager
{
    readonly PorterConfig config;
    readonly AwsEvents events;
    readonly AwsKms kms;
    readonly ILogger<AwsResourceManager> logger;
    readonly AwsSns sns;
    readonly AwsSqs sqs;

    public AwsResourceManager(
        ILogger<AwsResourceManager> logger,
        IOptions<PorterConfig> config,
        AwsEvents events,
        AwsSns sns,
        AwsSqs sqs,
        AwsKms kms
    )
    {
        this.config = config.Value;
        this.logger = logger;
        this.events = events;
        this.sns = sns;
        this.sqs = sqs;
        this.kms = kms;
    }

    public async ValueTask EnsureQueueExists(string topic,
        TopicNameOverride? nameOverride,
        CancellationToken ct)
    {
        TopicId topicId = new(topic, config.FromOverride(nameOverride));

        if (nameOverride?.HasValues() == true)
        {
            TopicId original = new(topic, config);
            logger.LogInformation(
                "Overriding queue name from \'{OriginalQueueName}\' to \'{TopicIdQueueName}\'",
                original.QueueName, topicId.QueueName);
        }

        logger.LogInformation("Setting queue '{Queue}' up: Region={Region}",
            topicId.QueueName,
            config.RegionEndpoint().SystemName);

        if (await sqs.QueueExists(topicId.QueueName, ct))
            return;

        var topicArn = await sns.EnsureTopic(topicId, ct);
        var queueInfo = await sqs.CreateQueue(topicId.QueueName, ct);
        logger.LogInformation(
            "Subscribing {TopicIdQueueName}[{QueueInfoArn}] on {TopicIdTopicName}[{TopicArn}]",
            topicId.QueueName, queueInfo.Arn, topicId.TopicName, topicArn);
        await sns.Subscribe(topicArn, queueInfo.Arn, ct);

        await WaitForQueue(topicId.QueueName, ct)
            .WaitAsync(TimeSpan.FromMinutes(5), ct);
    }

    public async ValueTask UpdateQueueAttr(
        string topic,
        TimeSpan? newTimeout,
        TopicNameOverride? nameOverride,
        CancellationToken ct)
    {
        TopicId topicId = new(topic, config.FromOverride(nameOverride));

        if (newTimeout is null)
            return;
        await sqs.UpdateQueueAttributes(topicId.QueueName, newTimeout.Value, ct);
    }

    async Task WaitForQueue(string queueName, CancellationToken ct)
    {
        while (await sqs.GetQueue(queueName, ct) is null)
        {
            logger.LogInformation("Waiting queue be available...");
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            logger.LogInformation("Not available yet");
        }

        logger.LogInformation("Queue available!");
    }

    public async ValueTask EnsureTopicExists(string topic,
        TopicNameOverride? nameOverride,
        CancellationToken ct)
    {
        var overrideConfig = config.FromOverride(nameOverride);
        TopicId topicId = new(topic, overrideConfig);

        if (nameOverride?.HasValues() == true)
        {
            TopicId original = new(topic, config);
            logger.LogInformation(
                "Overriding topic name from \'{OriginalTopicName}\' to \'{TopicIdTopicName}\'",
                original.TopicName, topicId.TopicName);
        }

        await EnsureTopicExists(topicId, ct);
    }

    public async ValueTask EnsureTopicExists(TopicId topic,
        CancellationToken ct)
    {
        if (await events.RuleExists(topic, ct))
        {
            logger.LogInformation("Rule {TopicTopicName} already exists", topic.TopicName);
            return;
        }

        logger.LogInformation("Setting topic '{Topic}' up: Region={Region}", topic.Event,
            config.RegionEndpoint().SystemName);

        if (!config.AutoCreateNewTopic)
            throw new InvalidOperationException(
                $"Topic '{topic.TopicName}' for '{topic.Event}' does not exists");

        await events.CreateRule(topic, ct);
        var topicArn = await sns.EnsureTopic(topic, ct);
        await events.PutTarget(topic, topicArn, ct);
    }

    public async Task SetupLocalstack(CancellationToken ct)
    {
        var keyId = await kms.GetKey(ct);
        if (keyId is null)
            await kms.CreteKey();
    }
}
