using AutoBogus;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Porter.Testing;

namespace Porter.Testing.Tests;

public class FakeBrokerAssertionsTests
{
    IServiceProvider provider = null!;
    IFakeBroker broker = null!;

    [SetUp]
    public void Setup()
    {
        provider = new ServiceCollection()
            .AddLogging()
            .AddPorter(nav =>
            {
                nav.Source = "test";

                nav.MapTopic<MyMessage1>("my_topic");

                nav.MapTopic<MyMessage2>("my_recur_topic")
                    .WithConsumer(async (MyMessage2 message, IProducer<MyMessage1> producer) =>
                    {
                        var message2 = new MyMessage1 { Id = message.Id, Foo = message.Bar };
                        await producer.TryPublish(message2);
                    });
            })
            .MockPorter()
            .BuildServiceProvider();

        broker = provider.GetRequiredService<IFakeBroker>();
        broker.Reset();
        broker.AutoConsumeLoop();
    }

    [Test]
    public async Task ShouldCheckProducedMessage()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message = AutoFaker.Generate<MyMessage1>();
        await publisher.TryPublish(message);

        broker.Should().ContainsAnyMessage("my_topic");
    }

    [Test]
    public void ShouldThrowIfHaveNoProducedMessage()
    {
        var action = () => broker.Should().ContainsAnyMessage("my_topic");
        action.Should().Throw<AssertionException>()
            .WithMessage(@"Expected ""my_topic"" to contain messages, but not found any.");
    }

    [Test]
    public async Task ShouldCheckForProducedMessageBody()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message = AutoFaker.Generate<MyMessage1>();
        await publisher.TryPublish(message);

        broker.Should().ContainsMessage("my_topic", message.ToJson());
    }

    [Test]
    public async Task ShouldCheckForSerializedProducedMessage()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message = AutoFaker.Generate<MyMessage1>();
        await publisher.TryPublish(message);

        broker.Should().ContainsMessage("my_topic", new { id = message.Id, foo = message.Foo });
    }

    [Test]
    public async Task ShouldCheckForNotProducedMessageBody()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message = AutoFaker.Generate<MyMessage1>();
        await publisher.TryPublish(message);

        var action = () => broker.Should().NotContainsMessage("my_topic", message.ToJson());
        action.Should().Throw<AssertionException>();

        var message2 = AutoFaker.Generate<MyMessage1>();
        broker.Should().NotContainsMessage("my_topic", message2.ToJson());
    }

    [Test]
    public async Task ShouldCheckForNotSerializedProducedMessage()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message = AutoFaker.Generate<MyMessage1>();
        await publisher.TryPublish(message);

        var action = () => broker.Should().NotContainsMessage("my_topic", new { id = message.Id, foo = message.Foo });
        action.Should().Throw<AssertionException>();

        var message2 = AutoFaker.Generate<MyMessage1>();
        broker.Should().NotContainsMessage("my_topic", new { id = message2.Id, foo = message2.Foo });
    }

    [Test]
    public async Task ShouldCheckForPartialMessage()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message = AutoFaker.Generate<MyMessage1>();
        await publisher.TryPublish(message);

        broker.Should().ContainsMessageEquivalentTo("my_topic", new { id = message.Id });
    }

    [Test]
    public async Task ShouldCheckForPartialJsonMessage()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message = AutoFaker.Generate<MyMessage1>();
        await publisher.TryPublish(message);

        broker.Should().ContainsMessageEquivalentTo("my_topic", $@"{{""id"":""{message.Id}""}}");
    }

    [Test]
    public async Task ShouldThrowIfHaveNoProducedJsonMessageOnTopic()
    {
        var message1 = AutoFaker.Generate<MyMessage1>();
        var message2 = AutoFaker.Generate<MyMessage1>();
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();
        await publisher.TryPublish(message1);

        var action = () => broker.Should().ContainsMessage("my_topic", message2.ToJson());

        action.Should().Throw<AssertionException>();
    }

    [Test]
    public async Task ShouldThrowIfHaveNoProducedMessageOnTopic()
    {
        var message1 = AutoFaker.Generate<MyMessage1>();
        var message2 = AutoFaker.Generate<MyMessage1>();
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();
        await publisher.TryPublish(message1);

        var action = () => broker.Should().ContainsMessage("my_topic", message2);
        action.Should().Throw<AssertionException>();
    }

    [Test]
    public async Task ShouldThrowIfHaveNoProducedAnonymousMessageOnTopic()
    {
        var message1 = AutoFaker.Generate<MyMessage1>();
        var message2 = AutoFaker.Generate<MyMessage1>();
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();
        await publisher.TryPublish(message1);

        var action = () => broker.Should().ContainsMessage("my_topic", new { id = message2.Id, foo = message2.Foo });

        action.Should().Throw<AssertionException>();
    }

    [Test]
    public async Task ShouldCheckForProducedMessages()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.TryPublish(message);

        broker.Should().AllMessagesBe(new()
        {
            ["my_recur_topic"] = new[] { message },
            ["my_topic"] = new[] { new MyMessage1 { Foo = message.Bar, Id = message.Id } },
        });
    }

    [Test]
    public async Task ShouldCheckForNotProducedMessages()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage2, MyMessage1>>();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.TryPublish(message);

        var action = () => broker.Should().AllMessagesNotBe(new()
        {
            ["my_recur_topic"] = new[] { message },
            ["my_topic"] = new[] { new MyMessage1 { Foo = message.Bar, Id = message.Id } },
        });

        action.Should().Throw<AssertionException>();
    }

    [Test]
    public async Task ShouldCheckForEquivalentProducedMessages()
    {
        broker.AutoConsumeLoop();
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.TryPublish(message);

        broker.Should().AllMessageBeEquivalentTo(new()
        {
            ["my_recur_topic"] = new[] { new { id = message.Id, bar = message.Bar } },
            ["my_topic"] = new[] { new { id = message.Id, foo = message.Bar } },
        });
    }

    [Test]
    public async Task ShouldCheckForObjectProducedMessages()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.TryPublish(message);

        broker.Should().AllMessageBeEquivalentTo(new
        {
            my_recur_topic = new[] { new { id = message.Id, bar = message.Bar } },
            my_topic = new[] { new { id = message.Id, foo = message.Bar } },
        });
    }

    [Test]
    public async Task ShouldCheckForObjectNotProducedMessages()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.TryPublish(message);

        var action = () => broker.Should().AllMessagesNotBe(new
        {
            my_recur_topic = new[] { new { id = message.Id, bar = message.Bar } },
            my_topic = new[] { new { id = message.Id, foo = message.Bar } },
        });

        action.Should().Throw<AssertionException>();

        broker.Should().AllMessagesNotBe(new { my_topic = new[] { new { id = Guid.NewGuid(), foo = "message" } } });
    }

    [Test]
    public async Task ShouldCheckForPartialObjectProducedMessages()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.TryPublish(message);

        broker.Should().AllMessageBeEquivalentTo(new
        {
            my_recur_topic = new[] { new { id = message.Id } },
            my_topic = new[] { new { foo = message.Bar } },
        });
    }

    [Test]
    public async Task ShouldCheckForPartialJsonMessages()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.TryPublish(message);

        broker.Should()
            .AllJsonMessagesBeEquivalentTo(new() { ["my_recur_topic"] = new[] { $@"{{""id"":""{message.Id}""}}" } });
    }

    [Test]
    public async Task ShouldCheckForPartialObjectMessage()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.TryPublish(message);

        broker.Should().AllMessageBeEquivalentTo(new() { ["my_recur_topic"] = new[] { new { id = message.Id } } });
    }

    [Test]
    public async Task ShouldCheckWhenDeltaProducedMessages()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message1 = AutoFaker.Generate<MyMessage1>();
        await publisher.TryPublish(message1);
        var message2 = AutoFaker.Generate<MyMessage1>();

        await broker
            .Should()
            .When(() => publisher.TryPublish(message2))
            .ContainMessage("my_topic", new { id = message2.Id, foo = message2.Foo });
    }

    [Test]
    public async Task ShouldCheckWhenDeltaProducedMessage()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message1 = AutoFaker.Generate<MyMessage1>();
        await publisher.TryPublish(message1);
        var message2 = AutoFaker.Generate<MyMessage1>();

        await broker
            .Should()
            .When(() => publisher.TryPublish(message2))
            .MessagesBe(new { my_topic = new[] { new { id = message2.Id, foo = message2.Foo } } });
    }

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Randomizer.Seed = new Random(42);
        AutoFaker.Configure(builder => builder
            .WithRecursiveDepth(1)
            .WithRepeatCount(1));
    }
}
