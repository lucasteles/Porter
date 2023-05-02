using System.Diagnostics;
using AutoBogus;
using Bogus;
using Microsoft.Extensions.DependencyInjection;

namespace Navi.Aws.Tests.TestUtils.Fixtures;

public class BaseTest
{
    protected static readonly Faker faker = new("pt_BR");
    protected AutoFakeIt mocker = null!;

    [OneTimeSetUp]
    public void SetUpOneTimeBase()
    {
        Randomizer.Seed = new Random(42);
        AutoFaker.Configure(builder => builder
            .WithRecursiveDepth(1)
            .WithRepeatCount(1));

        AssertionOptions.AssertEquivalencyUsing(options => options
            .Using<DateTime>(ctx =>
                ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromMilliseconds(100)))
            .WhenTypeIs<DateTime>()
            .Using<decimal>(ctx =>
                ctx.Subject.Should().BeApproximately(ctx.Expectation, .0001M))
            .WhenTypeIs<decimal>()
            .Using<double>(ctx =>
                ctx.Subject.Should().BeApproximately(ctx.Expectation, .0001))
            .WhenTypeIs<double>());
    }

    [SetUp]
    public void SetupBase()
    {
        mocker = new();
        MockServiceProvider(mocker);
    }

    static void MockServiceProvider(AutoFakeIt autoFake)
    {
        var fakeProvider = autoFake.Resolve<IServiceProvider>();
        var scopeFactory = autoFake.Resolve<IServiceScopeFactory>();
        var scope = autoFake.Resolve<IServiceScope>();

        A.CallTo(() => fakeProvider.GetService(A<Type>._))
            .ReturnsLazily(v => autoFake
                .Resolve(v.Arguments.Get<Type>("serviceType")!));

        A.CallTo(() => scopeFactory.CreateScope()).Returns(scope);
        A.CallTo(() => scope.ServiceProvider).Returns(fakeProvider);
    }

    [OneTimeSetUp]
    public void StartTest() => Trace.Listeners.Add(new ConsoleTraceListener());

    [OneTimeTearDown]
    public void EndTest() => Trace.Flush();
}
