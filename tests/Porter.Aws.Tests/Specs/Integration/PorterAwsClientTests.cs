using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Porter;
using Porter.Aws.Tests.Builders;
using Porter.Aws.Tests.TestUtils;
using Porter.Aws.Tests.TestUtils.Fixtures;
using Porter.Clients;
using Porter.Models;
using Porter.Services;

namespace Porter.Aws.Tests.Specs.Integration;

public class PorterClientTests : PorterClientFixture
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddSingleton<IRetryStrategy, NoRetryStrategy>();
    }

    [Test]
    public async Task ShouldSendAndReceiveMessages()
    {
        var message = TestMessage.New().ToJson();
        var correlationId = Guid.NewGuid();

        var client = (PorterAwsClient)GetService<IPorterClient>();
        var published =
            await client.Publish(Topic, message, correlationId, default);

        await WaitFor(() => sqs.HasMessagesOn(Topic));
        var messages = await client.Receive(Topic, default);

        messages.ShouldMessagesBeEquivalentTo(new[]
        {
            new
            {
                Body = JsonDocument.Parse(message),
                Datetime = fakedDate,
                published.MessageId,
            },
        });
    }

    [Test]
    public async Task ShouldSendAndReceiveMessagesOnClassPublicApi()
    {
        var stringTopicName = Topic.Event;

        var correlationId = Guid.NewGuid();
        var message = TestMessage.New().ToJson();

        var client = GetService<IPorterClient>();
        var published = await client.Publish(stringTopicName, message, correlationId);

        var messages = await client.Receive(stringTopicName);

        messages.ShouldMessagesBeEquivalentTo(new[]
        {
            new
            {
                Body = JsonDocument.Parse(message),
                Datetime = fakedDate,
                published.MessageId,
                CorrelationId = correlationId,
            },
        });
    }

    [Test]
    public async Task ShouldSendAndReceiveSerializedMessages()
    {
        var message = TestMessage.New();
        var correlationId = Guid.NewGuid();

        var stringTopicName = Topic.Event;

        var client = GetService<IPorterClient>();
        await client.Publish(stringTopicName, message, correlationId);

        var messages = await client.Receive<TestMessage>(stringTopicName);

        messages.Should()
            .BeEquivalentTo(new[]
            {
                new
                {
                    Body = message,
                    Datetime = fakedDate,
                    CorrelationId = correlationId,
                },
            });
    }

    [Test]
    public async Task ShouldDeliverMessagesToAllConsumers()
    {
        var message = TestMessage.New();

        var producer = await CreateProducer();
        var consumer1 = GetService<IConsumerClient>();
        var consumer2 = await CreateConsumer();
        var consumer3 = await CreateConsumer();

        var published = await producer.Publish(TopicName, message);

        await WaitFor(() => sqs.HasMessagesOn(Topic.QueueName));
        var messages1 = await consumer1.Receive<TestMessage>(TopicName);
        await Task.Delay(1000);
        var messages2 = await consumer2.Receive<TestMessage>(TopicName);
        var messages3 = await consumer3.Receive<TestMessage>(TopicName);

        var expected = new[]
        {
            new
            {
                Body = message,
                Datetime = fakedDate,
                published.MessageId,
            },
        };
        messages1.Should().BeEquivalentTo(expected);
        messages2.Should().BeEquivalentTo(expected);
        messages3.Should().BeEquivalentTo(expected);
    }

    [Test]
    public async Task ShouldDeserializeSnakeCaseByDefault()
    {
        var strongMessage = TestMessage.New();
        var message = strongMessage.ToSnakeCaseJson();

        var stringTopicName = Topic.Event;

        var client = GetService<IPorterClient>();
        await client.Publish(stringTopicName, message);

        var messages = await client.Receive<TestMessage>(stringTopicName);

        messages.Should().BeEquivalentTo(new[]
        {
            new
            {
                Body = strongMessage,
                Datetime = fakedDate,
            },
        });
    }

    [Test]
    public async Task ShouldSerializeSnakeCaseByDefault()
    {
        var message = TestMessage.New();
        var jsonMessage = message.ToSnakeCaseJson().AsJToken();
        var stringTopicName = Topic.Event;

        var client = GetService<IPorterClient>();
        await client.Publish(stringTopicName, message);

        await WaitFor(() => sqs.HasMessagesOn(Topic));
        var messages = await client.Receive(stringTopicName);

        messages.Single().Body.AsJToken()
            .Should()
            .BeEquivalentTo(jsonMessage);
    }
}
