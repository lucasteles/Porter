using System.Collections.Immutable;
using System.Reflection;
using Navi;
using Navi.Aws.Tests.Builders;
using Navi.Aws.Tests.TestUtils;
using Navi.Aws.Tests.TestUtils.Fixtures;
using Navi.Hosting;
using Navi.Hosting.Job;
using Navi.Models;
using Navi.Services;

namespace Navi.Aws.Tests.Specs.Unit.Hosting;

public class NaviHostedServiceTests : BaseTest
{
    [Test]
    public void ShouldThrowIfTopicIsDuplicated()
    {
        var builder = new ConsumerDescriberBuilder()
            .WithTopicName(faker.TopicNameString());

        mocker.Provide<IEnumerable<IConsumerDescriber>>(new[]
        {
            builder.Generate(), builder.Generate(),
        });
        var action = () => mocker.Generate<NaviHostedService>();

        action.Should().Throw<ArgumentException>()
            .WithInnerException<TargetInvocationException>()
            .WithInnerException<NaviException>()
            .WithMessage("Duplicated topic definition");
    }

    [Test]
    public async Task BootstrapShouldBeCalledForEachDescriber()
    {
        var describers = GetConsumerDescribers();

        mocker.Provide<IEnumerable<IConsumerDescriber>>(describers);

        var service = mocker.Generate<NaviHostedService>();

        await service.StartAsync(default);
        await Task.Delay(100);

        var bootstrapper = mocker.Resolve<INaviResourceManager>();
        foreach (var describer in describers)
            A.CallTo(() => bootstrapper.EnsureTopicExists(
                    describer.TopicName, A<TopicNameOverride>._, A<CancellationToken>._))
                .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task BootstrapStartShouldStartConsumerJob()
    {
        var describers = GetConsumerDescribers();

        mocker.Provide<IEnumerable<IConsumerDescriber>>(describers);

        var service = mocker.Generate<NaviHostedService>();

        await service.StartAsync(default);

        var job = mocker.Resolve<IConsumerJob>();
        A.CallTo(() => job
                .Start(
                    A<ImmutableArray<IConsumerDescriber>>.That.IsSameSequenceAs(
                        describers),
                    A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    static IConsumerDescriber[] GetConsumerDescribers()
    {
        var builder = new ConsumerDescriberBuilder();
        return faker
            .Random
            .Items(FakeMessageTypes.All)
            .Select(types => builder
                .WithTopicName($"topic_{faker.Random.Guid():N}")
                .WithMessageType(types.Key)
                .WithConsumerType(types.Value)
                .Generate())
            .ToArray();
    }
}

static class FakeMessageTypes
{
    public static readonly Dictionary<Type, Type> All = new()
    {
        [typeof(First)] = typeof(IMessageConsumer<First>),
        [typeof(Seconds)] = typeof(IMessageConsumer<Seconds>),
        [typeof(Third)] = typeof(IMessageConsumer<Third>),
        [typeof(Fourth)] = typeof(IMessageConsumer<Fourth>),
        [typeof(Fifth)] = typeof(IMessageConsumer<Fifth>),
        [typeof(Sixth)] = typeof(IMessageConsumer<Sixth>),
        [typeof(Seventh)] = typeof(IMessageConsumer<Seventh>),
        [typeof(Eighth)] = typeof(IMessageConsumer<Eighth>),
        [typeof(Ninth)] = typeof(IMessageConsumer<Ninth>),
        [typeof(Tenth)] = typeof(IMessageConsumer<Tenth>),
    };

    public record First;

    public record Seconds;

    public record Third;

    public record Fourth;

    public record Fifth;

    public record Sixth;

    public record Seventh;

    public record Eighth;

    public record Ninth;

    public record Tenth;
}
