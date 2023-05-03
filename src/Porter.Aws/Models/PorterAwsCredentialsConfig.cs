using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Porter.Models;

public sealed class PorterAwsCredentialsConfig
{
    public const string AwsAccessKeyName = "PORTER_AWS_ACCESS_KEY_ID";
    public const string AwsSecretKeyName = "PORTER_AWS_SECRET_ACCESS_KEY";

    [ConfigurationKeyName(AwsAccessKeyName)]
    public string? PorterAwsAccessKey { get; set; }

    [ConfigurationKeyName(AwsSecretKeyName)]
    public string? PorterAwsSecretKey { get; set; }
}

public class PorterAwsCredentialsConfigOptions : IConfigureOptions<PorterAwsCredentialsConfig>
{
    readonly IConfiguration? configuration;

    public PorterAwsCredentialsConfigOptions(IConfiguration? configuration = null) =>
        this.configuration = configuration;

    public void Configure(PorterAwsCredentialsConfig options)
    {
        configuration?.Bind(options);

        var envAccessKey = Environment
            .GetEnvironmentVariable(PorterAwsCredentialsConfig.AwsAccessKeyName);
        var envSecretKey = Environment
            .GetEnvironmentVariable(PorterAwsCredentialsConfig.AwsSecretKeyName);

        if (envAccessKey is null or "" || envSecretKey is null or "") return;

        options.PorterAwsAccessKey = envAccessKey;
        options.PorterAwsSecretKey = envSecretKey;
    }
}
