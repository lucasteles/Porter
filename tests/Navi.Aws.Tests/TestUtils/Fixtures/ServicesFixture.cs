using Amazon.Runtime;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Navi.Models;
using Navi.Services;

namespace Navi.Aws.Tests.TestUtils.Fixtures;

public class ServicesFixture
{
    protected static readonly Faker faker = new();

    protected readonly INaviClock fakeClock = A.Fake<INaviClock>();

    ServiceProvider serviceProvider = null!;

    [SetUp]
    public async Task OneTimeSetupServicesTest()
    {
        ClearEnv();
        await BeforeSetup();

        var services = CreateNaviServices(ConfigureNavi);
        services.AddSingleton(fakeClock);
        ConfigureServices(services);
        serviceProvider = services.BuildServiceProvider();

        Fake.ClearConfiguration(fakeClock);
        Fake.ClearRecordedCalls(fakeClock);
    }

    public void ClearEnv()
    {
        Environment.SetEnvironmentVariable("NAVI_AWS_ACCESS_KEY_ID", null);
        Environment.SetEnvironmentVariable("NAVI_AWS_SECRET_ACCESS_KEY", null);
    }

    protected IServiceCollection CreateNaviServices(Action<NaviConfig> configure) =>
        new ServiceCollection()
            .AddLogging()
            .AddNaviServices(
                credentials: new AnonymousAWSCredentials(),
                config: configure);

    protected virtual Task BeforeSetup() => Task.CompletedTask;

    protected virtual void ConfigureNavi(NaviConfig c)
    {
        c.Source = "test";
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    [TearDown]
    public async Task OneTimeTearDownServicesTest() => await serviceProvider.DisposeAsync();

    public T GetService<T>() where T : notnull => serviceProvider.GetRequiredService<T>();
}
