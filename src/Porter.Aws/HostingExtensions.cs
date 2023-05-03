using Amazon.Runtime;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Porter.Hosting;
using Porter.Hosting.Config;
using Porter.Hosting.Job;
using Porter.Models;

namespace Porter;

public static class HostingExtensions
{
    public static IServiceCollection AddPorter(
        this IServiceCollection services,
        Action<PorterConfigBuilder>? config = null,
        AWSCredentials? credentials = null)
    {
        var builder = new PorterConfigBuilder(services);
        config?.Invoke(builder);
        builder.ConfigureServices();
        return services
            .AddPorterServices(builder.ConfigureOptions, credentials)
            .AddScoped<IPorterCorrelationIdContext, PorterCorrelationIdContext>()
            .AddScoped(typeof(IProducer<,>), typeof(TypedProducer<,>))
            .AddScoped(typeof(IProducer<,,>), typeof(TypedProducer<,,>))
            .AddSingleton<IConsumerFactory, ConsumerFactory>()
            .AddSingleton<IConsumerJob, ConcurrentConsumerJob>()
            .AddHostedService<PorterHostedService>();
    }

    public static void UsePorter(this IEndpointRouteBuilder app)
    {
        var settings = app.ServiceProvider.GetService<IOptions<PorterConfig>>();
        if (settings is null)
            throw new InvalidOperationException("You should call AddPorter before");

        app.MapTopicEndpoints(settings.Value);
    }
}
