using Amazon.Runtime;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Porter.Models;
using Porter.Services;

namespace Porter.Aws.Tests.TestUtils.Fixtures;

public class ServicesFixture
{
    protected static readonly Faker faker = new();

    protected readonly IPorterClock fakeClock = A.Fake<IPorterClock>();

    ServiceProvider serviceProvider = null!;

    [SetUp]
    public async Task OneTimeSetupServicesTest()
    {
        ClearEnv();
        await BeforeSetup();

        var services = CreatePorterServices(ConfigurePorter);
        services.AddSingleton(fakeClock);
        ConfigureServices(services);
        serviceProvider = services.BuildServiceProvider();

        Fake.ClearConfiguration(fakeClock);
        Fake.ClearRecordedCalls(fakeClock);
    }

    public void ClearEnv()
    {
        Environment.SetEnvironmentVariable("PORTER_AWS_ACCESS_KEY_ID", null);
        Environment.SetEnvironmentVariable("PORTER_AWS_SECRET_ACCESS_KEY", null);
    }

    protected IServiceCollection CreatePorterServices(Action<PorterConfig> configure) =>
        new ServiceCollection()
            .AddLogging()
            .AddPorterServices(
                credentials: new AnonymousAWSCredentials(),
                config: configure);

    protected virtual Task BeforeSetup() => Task.CompletedTask;

    protected virtual void ConfigurePorter(PorterConfig c)
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
