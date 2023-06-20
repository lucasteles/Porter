using AutoBogus;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Porter.Testing;

namespace Porter.Testing.Tests;

public class FakeBrokerTests
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
    }

    [Test]
    public async Task ShouldReturnSerializedProducedMessage()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message = AutoFaker.Generate<MyMessage1>();
        await publisher.TryPublish(message);

        var produced = broker.ProducedOn<MyMessage1>("my_topic");

        produced.Should().BeEquivalentTo(new[] { message });
    }

    [Test]
    public async Task ShouldReturnProducedMessage()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message = AutoFaker.Generate<MyMessage1>();
        await publisher.TryPublish(message);

        var produced = broker.ProducedOn("my_topic");

        produced.Should().BeEquivalentTo(message.ToJson());
    }

    [Test]
    public async Task ShouldReturnProducesMessages()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message1 = AutoFaker.Generate<MyMessage1>();
        var message2 = AutoFaker.Generate<MyMessage1>();
        await publisher.TryPublish(message1);
        await publisher.TryPublish(message2);

        var produced = broker.ProducedOn("my_topic");

        produced.Should().BeEquivalentTo(message1.ToJson(), message2.ToJson());
    }

    [Test]
    public async Task ShouldReturnSerializedDeltaProducedMessage()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message1 = AutoFaker.Generate<MyMessage1>();
        await publisher.TryPublish(message1);
        var message2 = AutoFaker.Generate<MyMessage1>();

        var produced = await broker.Delta<MyMessage1>("my_topic", () => publisher.TryPublish(message2));

        produced.Should().BeEquivalentTo(new[] { message2 });
    }

    [Test]
    public async Task ShouldReturnDeltaProducedMessage()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage1>>();

        var message1 = AutoFaker.Generate<MyMessage1>();
        await publisher.TryPublish(message1);
        var message2 = AutoFaker.Generate<MyMessage1>();

        var produced = await broker.Delta("my_topic", () => publisher.TryPublish(message2));

        produced.Should().BeEquivalentTo(message2.ToJson());
    }

    [Test]
    public async Task ShouldReturnAllProducedMessage()
    {
        broker.AutoConsumeLoop();
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.TryPublish(message);

        var produced = broker.ProducedMessages();

        produced.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            ["my_recur_topic"] = new[] { message.ToJson() },
            ["my_topic"] = new[] { new MyMessage1 { Id = message.Id, Foo = message.Bar }.ToJson() },
        });
    }

    [Test]
    public async Task ShouldReturnAllProducedDeltaMessage()
    {
        broker.AutoConsumeLoop();
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();

        var message_old = AutoFaker.Generate<MyMessage2>();
        await publisher.TryPublish(message_old);

        var message = AutoFaker.Generate<MyMessage2>();

        var produced = await broker.Delta(() => publisher.TryPublish(message));

        produced.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            ["my_recur_topic"] = new[] { message.ToJson() },
            ["my_topic"] = new[] { new MyMessage1 { Id = message.Id, Foo = message.Bar }.ToJson() },
        });
    }

    [Test]
    public async Task ShouldReset()
    {
        var publisher = provider.GetRequiredService<IProducer<MyMessage2>>();
        broker.AutoConsumeLoop();

        var message = AutoFaker.Generate<MyMessage2>();
        await publisher.TryPublish(message);
        var produced = broker.ProducedMessages();
        produced.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            ["my_recur_topic"] = new[] { message.ToJson() },
            ["my_topic"] = new[] { new MyMessage1 { Id = message.Id, Foo = message.Bar }.ToJson() },
        });

        broker.Reset();
        broker.ProducedMessages().Should().BeEmpty();
    }

    [Test]
    public async Task ShouldPublish()
    {
        var message = AutoFaker.Generate<MyMessage2>();
        await broker.Produce("my_recur_topic", message.ToJson());

        var produced = broker.ProducedMessages();
        produced.Should().BeEquivalentTo(new Dictionary<string, string[]>
        {
            ["my_recur_topic"] = new[] { message.ToJson() },
            ["my_topic"] = new[] { new MyMessage1 { Id = message.Id, Foo = message.Bar }.ToJson() },
        });
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
