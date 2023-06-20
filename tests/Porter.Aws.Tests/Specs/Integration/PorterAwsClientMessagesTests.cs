using System.Diagnostics;
using System.Text.Json;
using Amazon.SQS.Model;
using Porter.Aws.Tests.Builders;
using Porter.Aws.Tests.TestUtils;
using Porter.Aws.Tests.TestUtils.Fixtures;
using Porter.Models;
using Porter.Services;

namespace Porter.Aws.Tests.Specs.Integration;

public class PorterClientMessageActionTests : PorterClientFixture
{
    protected override void ConfigurePorter(PorterConfig c)
    {
        base.ConfigurePorter(c);
        c.RaiseExceptions = true;
        c.RetriesBeforeDeadLetter = 2;
        c.MessageTimeoutInSeconds = 20;
    }

    [Test]
    public async Task ShouldDeleteMessageAfterProcessingIt()
    {
        var message = TestMessage.New().ToJson();

        var client = (PorterAwsClient)GetService<IPorterClient>();
        await client.Publish(Topic, message, null, default);

        var messages = await client.Receive(Topic, default);

        await messages.Single().Delete();

        (await sqs.GetMessageStats(Topic.QueueName))
            .Should().BeEquivalentTo((Processing: 0, Total: 0));
    }

    [Test]
    public async Task ShouldReleaseMessageBack()
    {
        var message = TestMessage.New().ToJson();

        var client = (PorterAwsClient)GetService<IPorterClient>();
        await client.Publish(Topic, message, null, default);

        await WaitFor(() => sqs.HasMessagesOn(Topic));
        var messages = await client.Receive(Topic, default);
        await messages.Single().Release(TimeSpan.Zero);

        (await sqs.HasMessagesOn(Topic.QueueName)).Should().BeTrue();
    }

    [Test]
    public async Task ShouldReleaseDelayMessage()
    {
        var message = TestMessage.New().ToJson();
        var watch = new Stopwatch();

        var delay = TimeSpan.FromSeconds(8);
        var client = (PorterAwsClient)GetService<IPorterClient>();

        await client.Publish(Topic, message, null, default);
        await WaitFor(() => sqs.HasMessagesOn(Topic));
        var messages = await client.Receive(Topic, default);
        await messages.Single().Release(delay);

        watch.Start();
        await Task.Delay(TimeSpan.FromSeconds(2));
        await WaitFor(async () => await sqs.HasMessagesOn(QueueName),
            next: TimeSpan.FromMilliseconds(100));
        watch.Stop();

        watch.Elapsed.Should().BeCloseTo(delay, TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task ShouldProcessRawMessage()
    {
        var messageString =
            """
             {
              "Type" : "Notification",
              "MessageId" : "684664c6-7082-53cc-b31c-3f3c62261def",
              "TopicArn" : "arn:aws:sns:sa-east-1:390294909665:TopicName",
              "Message" : "{\"event\":\"lead_analysis_completed\",\"datetime\":\"2022-11-25T00:46:28.328508+00:00\",\"payload\":{\"tax_id\":\"23519244000181\",\"request_id\":951,\"status\":\"SUCCESS\"}}",
              "Timestamp" : "2022-11-25T00:46:28.795Z",
              "SignatureVersion" : "1"
            }
            """;

        await sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = (await sqs.GetQueueUrlAsync(Topic.QueueName)).QueueUrl,
            MessageBody = messageString,
        });

        var messages = await GetService<IConsumerClient>().Receive(TopicName);

        messages.ShouldMessagesBeEquivalentTo(new[]
        {
            new
            {
                Body = JsonDocument.Parse(
                    "{\"tax_id\":\"23519244000181\",\"request_id\":951,\"status\":\"SUCCESS\"}"),
            },
        });
    }
}
