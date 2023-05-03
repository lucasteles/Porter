using Amazon.SQS;
using NUnit.Framework.Interfaces;
using Porter;
using Porter.Aws.Tests.TestUtils;
using Porter.Aws.Tests.TestUtils.Fixtures;
using Porter.Models;
using Porter.Services;
using TestMessage = Porter.Aws.Tests.Builders.TestMessage;

namespace Porter.Aws.Tests.Specs.Integration;

public class AwsClientNameOverrideTests : LocalstackFixture
{
    const string DefaultPrefix = "Prefix";
    const string DefaultSuffix = "Suffix";
    const string DefaultSource = "test";

    protected override void ConfigurePorter(PorterConfig c)
    {
        base.ConfigurePorter(c);
        c.Prefix = DefaultPrefix;
        c.Suffix = DefaultSuffix;
        c.Source = DefaultSource;
    }

    [Test]
    public async Task ShouldUseCustomTopicPrefix()
    {
        const string customPrefix = "Custom";
        const string topicName = "myTopic";
        var client = GetService<IPorterClient>();
        var nameOverride = new TopicNameOverride
        {
            Prefix = customPrefix,
        };
        var message = Builders.TestMessage.New().ToJson();

        await GetService<IPorterResourceManager>()
            .EnsureQueueExists(topicName, nameOverride, default);

        await client.Publish(topicName, message, null,
            new()
            {
                NameOverride = nameOverride,
            });

        var topic = new TopicId(topicName, new PorterTopicNameConfig
        {
            Source = DefaultSource,
            Suffix = DefaultSuffix,
            Prefix = customPrefix,
        });

        var sqs = GetService<IAmazonSQS>();
        await WaitFor(() => sqs.HasMessagesOn(topic));
        var received = await sqs.GetMessages(GetService<IPorterMessageSerializer>(), topic);

        var payload = received.Single().Payload.ToJsonString();
        payload.Should().Be(message);
    }

    [Test]
    public async Task ShouldUseCustomTopicSuffix()
    {
        const string customSuffix = "Custom";
        const string topicName = "myTopic";
        var client = GetService<IPorterClient>();
        var nameOverride = new TopicNameOverride
        {
            Suffix = customSuffix,
        };
        var message = Builders.TestMessage.New().ToJson();

        await GetService<IPorterResourceManager>()
            .EnsureQueueExists(topicName, nameOverride, default);

        await client.Publish(topicName, message, null,
            new()
            {
                NameOverride = nameOverride,
            });

        var topic = new TopicId(topicName, new PorterTopicNameConfig
        {
            Source = DefaultSource,
            Prefix = DefaultPrefix,
            Suffix = customSuffix,
        });

        var sqs = GetService<IAmazonSQS>();
        await WaitFor(() => sqs.HasMessagesOn(topic));
        var received = await sqs.GetMessages(GetService<IPorterMessageSerializer>(), topic);

        var payload = received.Single().Payload.ToJsonString();
        payload.Should().Be(message);
    }
}
