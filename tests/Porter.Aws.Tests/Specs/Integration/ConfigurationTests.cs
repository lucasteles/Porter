using AutoBogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Porter.Aws.Tests.TestUtils.Fixtures;
using Porter.Models;

namespace Porter.Aws.Tests.Specs.Integration;

public class ConfigurationHostTests : ServicesFixture
{
    readonly string appName = Guid.NewGuid().ToString("N");

    protected override void ConfigurePorter(PorterConfig c) { }

    protected override void ConfigureServices(IServiceCollection services)
    {
        var env = A.Fake<IHostEnvironment>();
        env.ApplicationName = appName;
        services.AddSingleton(env);
    }

    [Test]
    public void ShouldInferSourceNameByAssembly()
    {
        var porterConfig = GetService<IOptions<PorterConfig>>().Value;
        porterConfig.Source.Should().Be(appName);
    }

    [Test]
    public void ShouldFallbackRegionToEnvironmentVariable()
    {
        var region = faker.Address.State();
        Environment.SetEnvironmentVariable("PORTER_AWS_REGION", region,
            EnvironmentVariableTarget.Process);
        var porterConfig = GetService<IOptions<PorterConfig>>().Value;
        porterConfig.Region.Should().Be(region);
    }

    [TearDown]
    public void TearDown() =>
        Environment.SetEnvironmentVariable("PORTER_AWS_REGION", null,
            EnvironmentVariableTarget.Process);
}

public class ConfigurationTests : ServicesFixture
{
    readonly PorterConfig randomConfig = new AutoFaker<PorterConfig>()
        .RuleFor(x => x.Region, "us-east-1")
        .Generate();

    protected override void ConfigureServices(IServiceCollection services)
    {
        var mockSettings = randomConfig.GetType()
            .GetProperties()
            .ToDictionary(
                p => $"Porter:{p.Name}",
                p => p.GetValue(randomConfig)?.ToString()
            );

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(mockSettings)
            .Build();
        services.AddSingleton<IConfiguration>(_ => configuration!);
    }

    protected override void ConfigurePorter(PorterConfig c) { }

    [Test]
    public void ShouldUseIConfigurationFromContainer()
    {
        var porterConfig = GetService<IOptions<PorterConfig>>().Value;
        porterConfig.Should().BeEquivalentTo(randomConfig);
    }
}
