using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Porter.Aws.Tests.TestUtils;
using Porter.Aws.Tests.TestUtils.Fixtures;
using Porter.Extensions;
using Porter.Services;

namespace Porter.Aws.Tests.Specs.Integration;

public class AwsResourceManagerTests : LocalstackFixture
{
    [Test]
    public async Task ShouldCreateAllTopicResources()
    {
        var topicName = faker.TopicNameString();
        var bootstrapper = GetService<IPorterResourceManager>();

        await bootstrapper.EnsureTopicExists(topicName, new(), default);

        var resources = await GetResources();
        resources.Topic.Single().TopicArn.Should().Contain(topicName.ToPascalCase());
        resources.Rule.Single().Name.Should().Contain(topicName.ToPascalCase());
        resources.Queues.Should().BeEmpty();
    }

    [Test]
    public async Task ShouldCreateAllQueueResources()
    {
        var topicName = faker.TopicNameString();
        var bootstrapper = GetService<IPorterResourceManager>();

        await bootstrapper.EnsureQueueExists(topicName, new(), default);

        var resources = await GetResources();
        resources.Rule.Should().BeEmpty();
        resources.Topic.Single().TopicArn.Should().Contain(topicName.ToPascalCase());
        resources.Queues.Should().Contain(x => x.Contains(topicName));
    }

    public async Task<(Rule[] Rule, Topic[] Topic, string[] Queues)> GetResources()
    {
        var ev = GetService<IAmazonEventBridge>();
        var sns = GetService<IAmazonSimpleNotificationService>();
        var sqs = GetService<IAmazonSQS>();

        var savedRules = ev.ListRulesAsync(new ListRulesRequest());
        var savedTopics = sns.ListTopicsAsync(new ListTopicsRequest());
        var savedQueues = sqs.ListQueuesAsync(new ListQueuesRequest());

        await Task.WhenAll(savedRules, savedTopics, savedQueues);

        return (
            savedRules.Result.Rules.ToArray(),
            savedTopics.Result.Topics.ToArray(),
            savedQueues.Result.QueueUrls.Select(Path.GetFileName).Cast<string>().ToArray()
        );
    }
}
