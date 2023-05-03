using Amazon.Runtime;
using AutoBogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Porter.Aws.Tests.TestUtils.Fixtures;
using Porter.Models;

namespace Porter.Aws.Tests.Specs.Integration;

public class PorterAwsCredentialsConfigConfigurationTests : ServicesFixture
{
    readonly PorterAwsCredentialsConfig randomConfig = AutoFaker.Generate<PorterAwsCredentialsConfig>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PORTER_AWS_ACCESS_KEY_ID"] = randomConfig.PorterAwsAccessKey,
                ["PORTER_AWS_SECRET_ACCESS_KEY"] = randomConfig.PorterAwsSecretKey,
            })
            .Build();

        services
            .AddPorterServices(c => c.Source = "app")
            .AddSingleton<IConfiguration>(_ => configuration!);
    }

    [Test]
    public void ShouldUseIConfigurationFromContainer()
    {
        var porterConfig = GetService<IOptions<PorterAwsCredentialsConfig>>().Value;
        porterConfig.Should().BeEquivalentTo(randomConfig);
    }

    [Test]
    public void ShouldReturnBasicCredentials()
    {
        var cred = GetService<PorterAwsCredentialWrapper>().Credentials;
        cred.Should().BeOfType<BasicAWSCredentials>()
            .Which.GetCredentials()
            .Should().BeEquivalentTo(new
            {
                AccessKey = randomConfig.PorterAwsAccessKey,
                SecretKey = randomConfig.PorterAwsSecretKey,
            });
    }
}

public class PorterAwsCredentialsConfigEnvironmentTests : ServicesFixture
{
    readonly PorterAwsCredentialsConfig randomConfig = AutoFaker.Generate<PorterAwsCredentialsConfig>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        Environment.SetEnvironmentVariable(PorterAwsCredentialsConfig.AwsAccessKeyName,
            randomConfig.PorterAwsAccessKey,
            EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(PorterAwsCredentialsConfig.AwsSecretKeyName,
            randomConfig.PorterAwsSecretKey, EnvironmentVariableTarget.Process);

        services
            .AddPorterServices();
    }

    [TearDown]
    protected void TearDown()
    {
        Environment.SetEnvironmentVariable(PorterAwsCredentialsConfig.AwsAccessKeyName, null,
            EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(PorterAwsCredentialsConfig.AwsSecretKeyName, null,
            EnvironmentVariableTarget.Process);
    }

    [Test]
    public void ShouldUseIConfigurationFromContainer()
    {
        var porterConfig = GetService<IOptions<PorterAwsCredentialsConfig>>().Value;
        porterConfig.Should().BeEquivalentTo(randomConfig);
    }

    [Test]
    public void ShouldReturnBasicCredentials()
    {
        var cred = GetService<PorterAwsCredentialWrapper>().Credentials;
        cred.Should().BeOfType<BasicAWSCredentials>()
            .Which.GetCredentials()
            .Should().BeEquivalentTo(new
            {
                AccessKey = randomConfig.PorterAwsAccessKey,
                SecretKey = randomConfig.PorterAwsSecretKey,
            });
    }
}

public class PorterAwsCredentialsConfigIgnoreTests : ServicesFixture
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .Build();

        services
            .AddSingleton<IConfiguration>(_ => configuration!)
            .AddPorterServices();
    }

    [Test]
    public void ShouldUseIConfigurationFromContainer()
    {
        var porterConfig = GetService<IOptions<PorterAwsCredentialsConfig>>().Value;
        porterConfig.Should().BeEquivalentTo(new PorterAwsCredentialsConfig
        {
            PorterAwsAccessKey = null,
            PorterAwsSecretKey = null,
        });
    }

    [Test]
    public void ShouldReturnBasicCredentials()
    {
        var cred = GetService<PorterAwsCredentialWrapper>().Credentials;
        cred.Should().NotBeOfType<BasicAWSCredentials>();
    }
}
