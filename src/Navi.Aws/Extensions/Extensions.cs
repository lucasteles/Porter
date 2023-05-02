using System.Globalization;
using System.Text;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Navi.Models;

namespace Navi.Extensions;

static class Extensions
{
    public static string ToPascalCase(this string snakeName) =>
        string.Concat(snakeName.ToLowerInvariant().Split('_')
            .Select(CultureInfo.InvariantCulture.TextInfo.ToTitleCase));

    public static string EncodeAsUtf8(this string myString)
    {
        var bytes = Encoding.Default.GetBytes(myString);
        return Encoding.UTF8.GetString(bytes);
    }

    public static string TrimUnderscores(this string text) => text.Trim('_');

    public static string ToSnakeCase(this string str)
        => string.Concat(str
                .Select((x, i) =>
                    i > 0 && i < str.Length - 1 && char.IsUpper(x) && !char.IsUpper(str[i - 1])
                        ? $"_{x}"
                        : x.ToString()))
            .ToLowerInvariant();

    public static Amazon.RegionEndpoint RegionEndpoint(this NaviConfig config) =>
        Amazon.RegionEndpoint.GetBySystemName(config.Region);

    public static AWSCredentials ResolveAwsCredential(this IServiceProvider sp,
        AWSCredentials? credentials)
    {
        if (credentials is not null)
            return credentials;

        if (sp.GetService<IOptions<NaviConfig>>() is { Value.Localstack: true })
            return new AnonymousAWSCredentials();

        if (sp.GetService<IOptions<NaviAwsCredentialsConfig>>() is
            {
                Value.NaviAwsAccessKey: { } awsAccessKey,
                Value.NaviAwsSecretKey: { } awsSecretKey,
            })
            return new BasicAWSCredentials(awsAccessKey, awsSecretKey);

        return FallbackCredentialsFactory.GetCredentials();
    }

    public static IServiceCollection AddAwsConfig<TConfig>(this IServiceCollection services)
        where TConfig : ClientConfig, new() =>
        services.AddTransient(sp =>
        {
            var subSettings = sp.GetRequiredService<IOptionsMonitor<NaviConfig>>().CurrentValue;
            var config = new TConfig();
            if (!string.IsNullOrWhiteSpace(subSettings.ServiceUrl))
                config.ServiceURL = subSettings.ServiceUrl;
            else
                config.RegionEndpoint = subSettings.RegionEndpoint();

            return config;
        });

    public static AWSCredentials GetAwsCredentials(this IServiceProvider provider) =>
        provider.GetRequiredService<NaviAwsCredentialWrapper>().Credentials;
}
