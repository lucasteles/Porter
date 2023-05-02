using AutoBogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Navi.Aws.Tests.TestUtils.Fixtures;
using Navi.Models;

namespace Navi.Aws.Tests.Specs.Integration;

public class ConfigurationHostTests : ServicesFixture
{
    readonly string appName = Guid.NewGuid().ToString("N");

    protected override void ConfigureNavi(NaviConfig c) { }

    protected override void ConfigureServices(IServiceCollection services)
    {
        var env = A.Fake<IHostEnvironment>();
        env.ApplicationName = appName;
        services.AddSingleton(env);
    }

    [Test]
    public void ShouldInferSourceNameByAssembly()
    {
        var naviConfig = GetService<IOptions<NaviConfig>>().Value;
        naviConfig.Source.Should().Be(appName);
    }

    [Test]
    public void ShouldFallbackRegionToEnvironmentVariable()
    {
        var region = faker.Address.State();
        Environment.SetEnvironmentVariable("NAVI_AWS_REGION", region,
            EnvironmentVariableTarget.Process);
        var naviConfig = GetService<IOptions<NaviConfig>>().Value;
        naviConfig.Region.Should().Be(region);
    }

    [TearDown]
    public void TearDown() =>
        Environment.SetEnvironmentVariable("NAVI_AWS_REGION", null,
            EnvironmentVariableTarget.Process);
}

public class ConfigurationTests : ServicesFixture
{
    readonly NaviConfig randomConfig = new AutoFaker<NaviConfig>()
        .RuleFor(x => x.Region, "us-east-1")
        .Generate();

    protected override void ConfigureServices(IServiceCollection services)
    {
        var mockSettings = randomConfig.GetType()
            .GetProperties()
            .ToDictionary(
                p => $"Navi:{p.Name}",
                p => p.GetValue(randomConfig)?.ToString()
            );

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(mockSettings)
            .Build();
        services.AddSingleton<IConfiguration>(_ => configuration!);
    }

    protected override void ConfigureNavi(NaviConfig c) { }

    [Test]
    public void ShouldUseIConfigurationFromContainer()
    {
        var naviConfig = GetService<IOptions<NaviConfig>>().Value;
        naviConfig.Should().BeEquivalentTo(randomConfig);
    }
}
