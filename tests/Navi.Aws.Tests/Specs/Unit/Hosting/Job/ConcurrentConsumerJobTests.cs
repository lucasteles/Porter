using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Navi;
using Navi.Aws.Tests.Builders;
using Navi.Aws.Tests.TestUtils.Fixtures;
using Navi.Hosting;
using Navi.Hosting.Job;
using Navi.Models;

namespace Navi.Aws.Tests.Specs.Unit.Hosting.Job;

[SuppressMessage("Minor Code Smell", "S3878:Arrays should not be created for params parameters")]
public class ConcurrentConsumerJobTests : BaseTest
{
    [Test]
    public async Task ShouldProcessOneMessage()
    {
        var consumer = new ConsumerDescriberBuilder().Generate();
        var message = new FakeMessageBuilder().Generate();
        var ctx = new CancellationTokenSource();

        A.CallTo(() => mocker.Resolve<IOptionsMonitor<NaviConfig>>().CurrentValue)
            .Returns(new NaviConfig
            {
                MessageTimeoutInSeconds = 100,
                PollingIntervalInSeconds = 0.1f,
            });

        A.CallTo(() => mocker
                .Resolve<IConsumerClient>()
                .Receive(consumer.TopicName,
                    A<TopicNameOverride>._,
                    A<CancellationToken>._))
            .ReturnsNextFromSequence(new[]
            {
                message,
            });

        A.CallTo(() => mocker.Resolve<IConsumerFactory>()
                .ConsumeScoped(A<IConsumerDescriber>._, A<IMessage>._,
                    A<CancellationToken>._))
            .Invokes(() => ctx.CancelAfter(100));

        var job = mocker.Generate<ConcurrentConsumerJob>();
        var workerTask = () => job.Start(new[]
        {
            consumer,
        }, ctx.Token);

        await workerTask.Should().ThrowAsync<OperationCanceledException>();

        A.CallTo(() =>
                mocker.Resolve<IConsumerFactory>()
                    .ConsumeScoped(consumer, message, A<CancellationToken>._))
            .MustHaveHappened();
    }

    [Test, Ignore("see later")]
    public async Task ShouldTimeoutIfMessageIsNotProcessedInVisibleTime()
    {
        var message = new FakeMessageBuilder().Generate();
        const int timeoutInSeconds = 1;

        var consumer = new ConsumerDescriberBuilder()
            .Generate();

        A.CallTo(() => mocker.Resolve<IOptionsMonitor<NaviConfig>>().CurrentValue)
            .Returns(new NaviConfig
            {
                MessageTimeoutInSeconds = timeoutInSeconds,
                PollingIntervalInSeconds = 0.1f,
                RaiseExceptions = true,
            });

        A.CallTo(() => mocker
                .Resolve<IConsumerClient>()
                .Receive(consumer.TopicName,
                    A<TopicNameOverride>._,
                    A<CancellationToken>._))
            .ReturnsNextFromSequence(new[]
            {
                message,
            });

        A.CallTo(() => mocker.Resolve<IConsumerFactory>()
                .ConsumeScoped(A<IConsumerDescriber>._, A<IMessage>._,
                    A<CancellationToken>._))
            .ReturnsLazily(async fake =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1.1));
                fake.Arguments.Get<CancellationToken>("ctx")
                    .ThrowIfCancellationRequested();
            });

        var ctx = new CancellationTokenSource();

        A.CallTo(mocker.Resolve<ILogger<ConcurrentConsumerJob>>())
            .Where(x => x.Arguments.Get<LogLevel>("logLevel") == LogLevel.Critical)
            .Invokes(() => ctx.Cancel());

        var job = mocker.Generate<ConcurrentConsumerJob>();
        var task = () => job.Start(new[]
        {
            consumer,
        }, ctx.Token);
        await task.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test, Ignore("see later")]
    public async Task ShouldNotConcurrentlyProcessMoreMessagesThanConfigured()
    {
        var consumer = new ConsumerDescriberBuilder().WithConcurrency(1).Generate();
        var message1 = new FakeMessageBuilder().Generate();
        var message2 = new FakeMessageBuilder().Generate();

        A.CallTo(() => mocker.Resolve<IOptionsMonitor<NaviConfig>>().CurrentValue)
            .Returns(new NaviConfig
            {
                MessageTimeoutInSeconds = 100,
                PollingIntervalInSeconds = 0.1f,
            });

        A.CallTo(() => mocker.Resolve<IConsumerClient>()
                .Receive(consumer.TopicName, A<TopicNameOverride>._,
                    A<CancellationToken>._))
            .ReturnsNextFromSequence(
                new[]
                {
                    message1,
                }, new[]
                {
                    message2,
                });

        var ctx = new CancellationTokenSource();
        var semaphore = new SemaphoreSlim(1);
        await semaphore.WaitAsync(ctx.Token);
        A.CallTo(() => mocker.Resolve<IConsumerFactory>()
                .ConsumeScoped(A<IConsumerDescriber>._, A<IMessage>._,
                    A<CancellationToken>._))
            .ReturnsLazily(() => semaphore.WaitAsync(ctx.Token));

        var job = mocker.Generate<ConcurrentConsumerJob>();
        var workerTask = () => job.Start(new[]
        {
            consumer,
        }, ctx.Token);
        ctx.CancelAfter(500);
        await workerTask.Should().ThrowAsync<OperationCanceledException>();

        A.CallTo(() => mocker.Resolve<IConsumerFactory>()
                .ConsumeScoped(consumer, message1, A<CancellationToken>._))
            .MustHaveHappened();

        A.CallTo(() => mocker.Resolve<IConsumerFactory>()
                .ConsumeScoped(consumer, message2, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}
