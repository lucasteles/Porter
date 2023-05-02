using Amazon.SQS;
using Navi;
using Navi.Aws.Tests.TestUtils;
using Navi.Aws.Tests.TestUtils.Fixtures;
using Navi.Models;
using Navi.Services;
using NUnit.Framework.Interfaces;
using TestMessage = Navi.Aws.Tests.Builders.TestMessage;

namespace Navi.Aws.Tests.Specs.Integration;

public class AwsClientNameOverrideTests : LocalstackFixture
{
    const string DefaultPrefix = "Prefix";
    const string DefaultSuffix = "Suffix";
    const string DefaultSource = "test";

    protected override void ConfigureNavi(NaviConfig c)
    {
        base.ConfigureNavi(c);
        c.Prefix = DefaultPrefix;
        c.Suffix = DefaultSuffix;
        c.Source = DefaultSource;
    }

    [Test]
    public async Task ShouldUseCustomTopicPrefix()
    {
        const string customPrefix = "Custom";
        const string topicName = "myTopic";
        var client = GetService<INaviClient>();
        var nameOverride = new TopicNameOverride
        {
            Prefix = customPrefix,
        };
        var message = Builders.TestMessage.New().ToJson();

        await GetService<INaviResourceManager>()
            .EnsureQueueExists(topicName, nameOverride, default);

        await client.Publish(topicName, message, null,
            new()
            {
                NameOverride = nameOverride,
            });

        var topic = new TopicId(topicName, new NaviTopicNameConfig
        {
            Source = DefaultSource,
            Suffix = DefaultSuffix,
            Prefix = customPrefix,
        });

        var sqs = GetService<IAmazonSQS>();
        await WaitFor(() => sqs.HasMessagesOn(topic));
        var received = await sqs.GetMessages(GetService<INaviMessageSerializer>(), topic);

        var payload = received.Single().Payload.ToJsonString();
        payload.Should().Be(message);
    }

    [Test]
    public async Task ShouldUseCustomTopicSuffix()
    {
        const string customSuffix = "Custom";
        const string topicName = "myTopic";
        var client = GetService<INaviClient>();
        var nameOverride = new TopicNameOverride
        {
            Suffix = customSuffix,
        };
        var message = Builders.TestMessage.New().ToJson();

        await GetService<INaviResourceManager>()
            .EnsureQueueExists(topicName, nameOverride, default);

        await client.Publish(topicName, message, null,
            new()
            {
                NameOverride = nameOverride,
            });

        var topic = new TopicId(topicName, new NaviTopicNameConfig
        {
            Source = DefaultSource,
            Prefix = DefaultPrefix,
            Suffix = customSuffix,
        });

        var sqs = GetService<IAmazonSQS>();
        await WaitFor(() => sqs.HasMessagesOn(topic));
        var received = await sqs.GetMessages(GetService<INaviMessageSerializer>(), topic);

        var payload = received.Single().Payload.ToJsonString();
        payload.Should().Be(message);
    }
}
