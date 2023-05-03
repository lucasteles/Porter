using Amazon.EventBridge;
using Amazon.KeyManagementService;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Porter.Clients;
using Porter.Extensions;
using Porter.Hosting;
using Porter.Models;
using Porter.Services;

namespace Porter;

public static class ServicesExtensions
{
    public static IServiceCollection AddPorterServices(
        this IServiceCollection services,
        Action<PorterConfig>? config = null,
        AWSCredentials? credentials = null)
    {
        services
            .AddSingleton<IConfigureOptions<PorterAwsCredentialsConfig>,
                PorterAwsCredentialsConfigOptions>()
            .AddSingleton<IConfigureOptions<PorterConfig>, ConfigurePorterOption>()
            .PostConfigure<PorterConfig>(c =>
            {
                config?.Invoke(c);
                if (c is { Localstack: true, ServiceUrl: null })
                    c.ServiceUrl = LocalStackDefaultUrl;

                if (string.IsNullOrWhiteSpace(c.Source))
                    throw new InvalidOperationException("Unable to infer the source name");
            });

        services
            .AddSingleton<
                PorterAwsCredentialWrapper>(sp => new(sp.ResolveAwsCredential(credentials)));

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
            .AddSingleton<IPorterClock, UtcClock>()
            .AddSingleton<TagsService>()
            .AddSingleton<IDiagnostics, Diagnostics>()
            .AddSingleton<IRetryStrategy, Power2RetryStrategy>()
            .AddSingleton<PorterDefaultJsonSerializerConverters>()
            .AddSingleton<IPorterJsonSerializerConverters, PorterDefaultJsonSerializerConverters>()
            .AddSingleton<IPorterJsonSerializerOptions, PorterJsonSerializerOptions>()
            .AddSingleton<IPorterMessageSerializer, PorterJsonSerializer>()
            .AddScoped<IPorterResourceManager, AwsResourceManager>()
            .AddScoped<IConsumeDriver, AwsSqs>()
            .AddScoped<IProduceDriver, AwsEvents>()
            .AddScoped<ICorrelationResolver, CorrelationResolver>()
            .AddScoped<IPorterClient, PorterAwsClient>()
            .AddScoped<IProducerClient>(sp => sp.GetRequiredService<IPorterClient>())
            .AddScoped<IConsumerClient>(sp => sp.GetRequiredService<IPorterClient>());

        return services;
    }

    const string LocalStackDefaultUrl = "http://localhost:4566";
}
