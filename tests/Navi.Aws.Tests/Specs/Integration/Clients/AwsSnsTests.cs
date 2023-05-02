using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Navi.Aws.Tests.TestUtils;
using Navi.Aws.Tests.TestUtils.Fixtures;
using Navi.Clients;
using Navi.Models;

namespace Navi.Aws.Tests.Specs.Integration.Clients;

public class AwsSnsTests : LocalstackFixture
{
    [Test]
    public async Task ShouldCreateNewTopic()
    {
        var topicName = faker.TopicName(config);
        var aws = GetService<AwsSns>();

        await aws.EnsureTopic(topicName, default);

        var sns = GetService<IAmazonSimpleNotificationService>();
        var topics = await sns.ListTopicsAsync();

        topics.Topics.Should().ContainSingle();
    }

    [Test]
    public async Task CreateTopicShouldBeIdempotent()
    {
        var topicName = faker.TopicName(config);
        var aws = GetService<AwsSns>();

        var response1 = await aws.EnsureTopic(topicName, default);
        var response2 = await aws.EnsureTopic(topicName, default);

        var sns = GetService<IAmazonSimpleNotificationService>();
        var topics = await sns.ListTopicsAsync();

        topics.Topics.Should().ContainSingle();
        response1.Should().Be(response2);
    }

    [Test]
    public async Task ShouldCreateNewTopicWithArn()
    {
        var topicName = faker.TopicName(config);
        var aws = GetService<AwsSns>();

        var result = await aws.EnsureTopic(topicName, default);

        var sns = GetService<IAmazonSimpleNotificationService>();
        var topic = await sns.GetTopicAttributesAsync(new GetTopicAttributesRequest { TopicArn = result.Value });

        topic.Attributes.Should().HaveCountGreaterThan(0);
    }

    [Test]
    public async Task ShouldSubscribeTopic()
    {
        var topicName = faker.TopicName(config);

        var sns = GetService<IAmazonSimpleNotificationService>();
        var sqs = GetService<IAmazonSQS>();
        var topic = await sns.CreateTopicAsync(new CreateTopicRequest { Name = topicName.TopicName });
        var queueResponse = await sqs.CreateQueueAsync(new CreateQueueRequest { QueueName = topicName.QueueName });
        var queue = await sqs.GetQueueAttributesAsync(queueResponse.QueueUrl,
            new List<string> { QueueAttributeName.QueueArn });

        var aws = GetService<AwsSns>();

        await aws.Subscribe(new SnsArn(topic.TopicArn), new SqsArn(queue.QueueARN), default);

        var subs = await sns.ListSubscriptionsAsync();
        subs.Subscriptions.Should().ContainEquivalentOf(new
        {
            topic.TopicArn,
            Protocol = "sqs",
            Endpoint = queue.QueueARN,
        });
    }
}
