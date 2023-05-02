using Amazon.EventBridge;
using Amazon.KeyManagementService;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Navi.Clients;
using Navi.Extensions;
using Navi.Hosting;
using Navi.Models;
using Navi.Services;

namespace Navi;

public static class ServicesExtensions
{
    public static IServiceCollection AddNaviServices(
        this IServiceCollection services,
        Action<NaviConfig>? config = null,
        AWSCredentials? credentials = null)
    {
        services
            .AddSingleton<IConfigureOptions<NaviAwsCredentialsConfig>,
                NaviAwsCredentialsConfigOptions>()
            .AddSingleton<IConfigureOptions<NaviConfig>, ConfigureNaviOption>()
            .PostConfigure<NaviConfig>(c =>
            {
                config?.Invoke(c);
                if (c is { Localstack: true, ServiceUrl: null })
                    c.ServiceUrl = LocalStackDefaultUrl;

                if (string.IsNullOrWhiteSpace(c.Source))
                    throw new InvalidOperationException("Unable to infer the source name");
            });

        services
            .AddSingleton<
                NaviAwsCredentialWrapper>(sp => new(sp.ResolveAwsCredential(credentials)));

        services
            .AddAwsConfig<AmazonSQSConfig>()
            .AddAwsConfig<AmazonEventBridgeConfig>()
            .AddAwsConfig<AmazonKeyManagementServiceConfig>()
            .AddAwsConfig<AmazonSimpleNotificationServiceConfig>();

        services
            .AddTransient<IAmazonSimpleNotificationService>(sp =>
                new AmazonSimpleNotificationServiceClient(
                    sp.GetAwsCredentials(),
                    sp.GetRequiredService<AmazonSimpleNotificationServiceConfig>()))
            .AddTransient<IAmazonSQS>(sp =>
                new AmazonSQSClient(sp.GetAwsCredentials(),
                    sp.GetRequiredService<AmazonSQSConfig>()))
            .AddTransient<IAmazonEventBridge>(sp =>
                new AmazonEventBridgeClient(sp.GetAwsCredentials(),
                    sp.GetRequiredService<AmazonEventBridgeConfig>()))
            .AddTransient<IAmazonKeyManagementService>(sp =>
                new AmazonKeyManagementServiceClient(sp.GetAwsCredentials(),
                    sp.GetRequiredService<AmazonKeyManagementServiceConfig>()));

        services
            .AddScoped<AwsKms>()
            .AddScoped<AwsEvents>()
            .AddScoped<AwsSqs>()
            .AddScoped<AwsSns>();

        services
            .AddSingleton<INaviClock, UtcClock>()
            .AddSingleton<TagsService>()
            .AddSingleton<IDiagnostics, Diagnostics>()
            .AddSingleton<IRetryStrategy, Power2RetryStrategy>()
            .AddSingleton<NaviDefaultJsonSerializerConverters>()
            .AddSingleton<INaviJsonSerializerConverters, NaviDefaultJsonSerializerConverters>()
            .AddSingleton<INaviJsonSerializerOptions, NaviJsonSerializerOptions>()
            .AddSingleton<INaviMessageSerializer, NaviJsonSerializer>()
            .AddScoped<INaviResourceManager, AwsResourceManager>()
            .AddScoped<IConsumeDriver, AwsSqs>()
            .AddScoped<IProduceDriver, AwsEvents>()
            .AddScoped<ICorrelationResolver, CorrelationResolver>()
            .AddScoped<INaviClient, NaviAwsClient>()
            .AddScoped<IProducerClient>(sp => sp.GetRequiredService<INaviClient>())
            .AddScoped<IConsumerClient>(sp => sp.GetRequiredService<INaviClient>());

        return services;
    }

    const string LocalStackDefaultUrl = "http://localhost:4566";
}
