using Amazon.Runtime;
using AutoBogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Navi.Aws.Tests.TestUtils.Fixtures;
using Navi.Models;

namespace Navi.Aws.Tests.Specs.Integration;

public class NaviAwsCredentialsConfigConfigurationTests : ServicesFixture
{
    readonly NaviAwsCredentialsConfig randomConfig = AutoFaker.Generate<NaviAwsCredentialsConfig>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NAVI_AWS_ACCESS_KEY_ID"] = randomConfig.NaviAwsAccessKey,
                ["NAVI_AWS_SECRET_ACCESS_KEY"] = randomConfig.NaviAwsSecretKey,
            })
            .Build();

        services
            .AddNaviServices(c => c.Source = "app")
            .AddSingleton<IConfiguration>(_ => configuration!);
    }

    [Test]
    public void ShouldUseIConfigurationFromContainer()
    {
        var naviConfig = GetService<IOptions<NaviAwsCredentialsConfig>>().Value;
        naviConfig.Should().BeEquivalentTo(randomConfig);
    }

    [Test]
    public void ShouldReturnBasicCredentials()
    {
        var cred = GetService<NaviAwsCredentialWrapper>().Credentials;
        cred.Should().BeOfType<BasicAWSCredentials>()
            .Which.GetCredentials()
            .Should().BeEquivalentTo(new
            {
                AccessKey = randomConfig.NaviAwsAccessKey,
                SecretKey = randomConfig.NaviAwsSecretKey,
            });
    }
}

public class NaviAwsCredentialsConfigEnvironmentTests : ServicesFixture
{
    readonly NaviAwsCredentialsConfig randomConfig = AutoFaker.Generate<NaviAwsCredentialsConfig>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        Environment.SetEnvironmentVariable(NaviAwsCredentialsConfig.AwsAccessKeyName,
            randomConfig.NaviAwsAccessKey,
            EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(NaviAwsCredentialsConfig.AwsSecretKeyName,
            randomConfig.NaviAwsSecretKey, EnvironmentVariableTarget.Process);

        services
            .AddNaviServices();
    }

    [TearDown]
    protected void TearDown()
    {
        Environment.SetEnvironmentVariable(NaviAwsCredentialsConfig.AwsAccessKeyName, null,
            EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(NaviAwsCredentialsConfig.AwsSecretKeyName, null,
            EnvironmentVariableTarget.Process);
    }

    [Test]
    public void ShouldUseIConfigurationFromContainer()
    {
        var naviConfig = GetService<IOptions<NaviAwsCredentialsConfig>>().Value;
        naviConfig.Should().BeEquivalentTo(randomConfig);
    }

    [Test]
    public void ShouldReturnBasicCredentials()
    {
        var cred = GetService<NaviAwsCredentialWrapper>().Credentials;
        cred.Should().BeOfType<BasicAWSCredentials>()
            .Which.GetCredentials()
            .Should().BeEquivalentTo(new
            {
                AccessKey = randomConfig.NaviAwsAccessKey,
                SecretKey = randomConfig.NaviAwsSecretKey,
            });
    }
}

public class NaviAwsCredentialsConfigIgnoreTests : ServicesFixture
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .Build();

        services
            .AddSingleton<IConfiguration>(_ => configuration!)
            .AddNaviServices();
    }

    [Test]
    public void ShouldUseIConfigurationFromContainer()
    {
        var naviConfig = GetService<IOptions<NaviAwsCredentialsConfig>>().Value;
        naviConfig.Should().BeEquivalentTo(new NaviAwsCredentialsConfig
        {
            NaviAwsAccessKey = null,
            NaviAwsSecretKey = null,
        });
    }

    [Test]
    public void ShouldReturnBasicCredentials()
    {
        var cred = GetService<NaviAwsCredentialWrapper>().Credentials;
        cred.Should().NotBeOfType<BasicAWSCredentials>();
    }
}
