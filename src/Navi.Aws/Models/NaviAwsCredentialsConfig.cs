using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Navi.Models;

public sealed class NaviAwsCredentialsConfig
{
    public const string AwsAccessKeyName = "NAVI_AWS_ACCESS_KEY_ID";
    public const string AwsSecretKeyName = "NAVI_AWS_SECRET_ACCESS_KEY";

    [ConfigurationKeyName(AwsAccessKeyName)]
    public string? NaviAwsAccessKey { get; set; }

    [ConfigurationKeyName(AwsSecretKeyName)]
    public string? NaviAwsSecretKey { get; set; }
}

public class NaviAwsCredentialsConfigOptions : IConfigureOptions<NaviAwsCredentialsConfig>
{
    readonly IConfiguration? configuration;

    public NaviAwsCredentialsConfigOptions(IConfiguration? configuration = null) =>
        this.configuration = configuration;

    public void Configure(NaviAwsCredentialsConfig options)
    {
        configuration?.Bind(options);

        var envAccessKey = Environment
            .GetEnvironmentVariable(NaviAwsCredentialsConfig.AwsAccessKeyName);
        var envSecretKey = Environment
            .GetEnvironmentVariable(NaviAwsCredentialsConfig.AwsSecretKeyName);

        if (envAccessKey is null or "" || envSecretKey is null or "") return;

        options.NaviAwsAccessKey = envAccessKey;
        options.NaviAwsSecretKey = envSecretKey;
    }
}
