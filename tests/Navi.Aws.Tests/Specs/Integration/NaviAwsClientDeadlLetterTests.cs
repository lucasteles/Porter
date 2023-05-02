using System.Text.Json;
using Navi;
using Navi.Aws.Tests.Builders;
using Navi.Aws.Tests.TestUtils;
using Navi.Aws.Tests.TestUtils.Fixtures;
using Navi.Models;

namespace Navi.Aws.Tests.Specs.Integration;

public class NaviClientDeadLetterTests : NaviClientFixture
{
    protected override void ConfigureNavi(NaviConfig c)
    {
        base.ConfigureNavi(c);
        c.RetriesBeforeDeadLetter = 1;
    }

    [Test]
    public async Task MessageShouldGoToDeadLetterQueue()
    {
        var message = TestMessage.New().ToJson();

        var client = GetService<INaviClient>();
        await client.Publish(TopicName, message);

        await WaitFor(() => sqs.HasMessagesOn(Topic));
        var messages = await client.Receive(TopicName);
        await messages.Single().Release(TimeSpan.Zero);

        await Task.Delay(1000);
        var messageRetries = await client.Receive(TopicName);
        messageRetries.Should().BeEmpty();

        await Task.Delay(1000);
        var deadMessages = await client.DeadLetters(TopicName);
        deadMessages.ShouldMessagesBeEquivalentTo(new[]
        {
            new
            {
                Body = message.ToJsonDocument(),
                Datetime = fakedDate,
            },
        });
    }

    [Test]
    public async Task ShouldDeserializeDeadLetter()
    {
        var message = TestMessage.New();

        var client = GetService<INaviClient>();
        await client.Publish(TopicName, message);

        await WaitFor(() => sqs.HasMessagesOn(Topic));
        var messages = await client.Receive(TopicName);
        await messages.Single().Release(TimeSpan.Zero);

        await WaitFor(() => sqs.HasMessagesOn(Topic));
        var messageRetries = await client.Receive(TopicName);
        messageRetries.Should().BeEmpty();

        await Task.Delay(1000);
        var deadMessages = await client.DeadLetters<TestMessage>(TopicName);
        deadMessages.Should().BeEquivalentTo(new[]
        {
            new
            {
                Body = message,
                Datetime = fakedDate,
            },
        });
    }
}

public class NaviClientRetryTests : NaviClientFixture
{
    protected override void ConfigureNavi(NaviConfig c)
    {
        base.ConfigureNavi(c);
        c.RetriesBeforeDeadLetter = 2;
    }

    [Test]
    public async Task MessageShouldGoToDeadLetterQueue()
    {
        var message = TestMessage.New().ToJson();
        var expectedMessage = new
        {
            Body = JsonDocument.Parse(message),
            Datetime = fakedDate,
            RetryNumber = 0,
        };

        var client = GetService<INaviClient>();
        await client.Publish(TopicName, message);

        var messages1 = await client.Receive(TopicName);
        await messages1.Single().Release(TimeSpan.Zero);
        messages1.ShouldMessagesBeEquivalentTo(new[]
        {
            expectedMessage,
        });

        var messages2 = await client.Receive(TopicName);
        await messages2.Single().Release(TimeSpan.Zero);
        messages2.ShouldMessagesBeEquivalentTo(new[]
        {
            expectedMessage with
            {
                RetryNumber = 1,
            },
        });

        var messageRetries = await client.Receive(TopicName);
        messageRetries.Should().BeEmpty();

        var deadMessages = await client.DeadLetters(TopicName);
        messages2.ShouldMessagesBeEquivalentTo(new[]
        {
            expectedMessage with
            {
                RetryNumber = 1,
            },
        });
        deadMessages.ShouldMessagesBeEquivalentTo(new[]
        {
            expectedMessage,
        });
    }
}
