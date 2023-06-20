using Porter.Aws.Tests.Builders;
using Porter.Aws.Tests.TestUtils.Fixtures;
using Porter.Hosting;
using Porter.Models;

namespace Porter.Aws.Tests.Specs.Unit.Hosting;

public class DelegateConsumerTests : BaseTest
{
    async Task ValidateDelegateConsumer(Delegate handler)
    {
        var provider = mocker.Resolve<IServiceProvider>();
        var consumer = new DelegateConsumer<TestMessage>(handler, provider);
        var message = TestMessage.New();
        var meta = message.GetMeta();
        var ct = new CancellationToken();

        await consumer.Consume(message, meta, ct);

        A.CallTo(() => mocker
                .Resolve<IAsyncFakeService>()
                .Work(message, ct))
            .MustHaveHappened();
    }

    [Test]
    public async Task ShouldConstructDelegateShouldThrowIfNoMessageTypePassed()
    {
        var action = () => ValidateDelegateConsumer(
            async (IAsyncFakeService c, CancellationToken ctx) =>
                await c.Work(null!, ctx));

        await action.Should()
            .ThrowAsync<PorterException>()
            .WithMessage("No parameter of type*");
    }

    [Test]
    public async Task ShouldConstructDelegateFuncConsumer() =>
        await ValidateDelegateConsumer(
            async (TestMessage m, IAsyncFakeService c, CancellationToken ctx) =>
                await c.Work(m, ctx));

    [Test]
    public async Task ShouldConstructDelegateActionConsumer() =>
        await ValidateDelegateConsumer(
            new Action<TestMessage, IAsyncFakeService, CancellationToken>((m, c, ctx) =>
            {
                c.Work(m, ctx).GetAwaiter().GetResult();
            }));

    [Test]
    public async Task ShouldConstructDelegateNonTaskFunc() =>
        await ValidateDelegateConsumer(
            (TestMessage m, IAsyncFakeService c, CancellationToken ctx) =>
            {
                c.Work(m, ctx).GetAwaiter().GetResult();
            });
}

public interface IAsyncFakeService : IAsyncFakeService<TestMessage>
{
}

public interface IAsyncFakeService<in T> where T : notnull
{
    Task Work(T message, CancellationToken ctx);
}
