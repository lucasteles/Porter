using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Porter.Models;

/// <summary>
///
/// </summary>
public class PorterTopicNameConfig
{
    /// <summary>
    ///
    /// </summary>
    public string Suffix { get; set; } = "";

    /// <summary>
    ///
    /// </summary>
    public string Prefix { get; set; } = "";

    /// <summary>
    ///
    /// </summary>
    public string Source { get; set; } = "";

    internal PorterTopicNameConfig FromOverride(TopicNameOverride? @override) =>
        new()
        {
            Source = Source,
            Prefix = @override?.Prefix ?? Prefix,
            Suffix = @override?.Suffix ?? Suffix,
        };
}

public class PorterConfig : PorterTopicNameConfig
{
    public int QueueMaxReceiveCount { get; set; } = 5;
    public int RetriesBeforeDeadLetter { get; set; } = 3;
    public string PubKey { get; set; } = "alias/PubSubKey";
    public int MessageRetentionInDays { get; set; } = 7;
    public int MessageTimeoutInSeconds { get; set; } = 30;
    public int MessageDelayInSeconds { get; set; }
    public double PollingIntervalInSeconds { get; set; } = 5;
    public string? ServiceUrl { get; set; }
    public bool Localstack { get; set; }
    public bool MapConsumerEndpoints { get; set; }
    public bool AutoCreateNewTopic { get; set; } = true;
    public bool RaiseExceptions { get; set; }
    public string Region { get; set; } = "us-east-1";
    public int LongPollingWaitInSeconds { get; set; }
}

public class ConfigurePorterOption : IConfigureOptions<PorterConfig>
{
    const string ConfigSection = "Porter";
    readonly IConfiguration? configuration;
    readonly IHostEnvironment? hostEnvironment;

    public ConfigurePorterOption(
        IConfiguration? configuration = null,
        IHostEnvironment? hostEnvironment = null)
    {
        this.hostEnvironment = hostEnvironment;
        this.configuration = configuration;
    }

    public void Configure(PorterConfig options)
    {
        configuration?.GetSection(ConfigSection).Bind(options);
        if (hostEnvironment is not null && string.IsNullOrWhiteSpace(options.Source))
            options.Source = hostEnvironment.ApplicationName;

        if (Environment.GetEnvironmentVariable("PORTER_AWS_REGION") is
            { Length: > 0 } region)
            options.Region = region;
    }
}

sealed record PorterAwsCredentialWrapper(AWSCredentials Credentials);
