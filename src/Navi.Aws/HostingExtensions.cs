using Amazon.Runtime;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Navi.Hosting;
using Navi.Hosting.Config;
using Navi.Hosting.Job;
using Navi.Models;

namespace Navi;

public static class HostingExtensions
{
    public static IServiceCollection AddNavi(
        this IServiceCollection services,
        Action<NaviConfigBuilder>? config = null,
        AWSCredentials? credentials = null)
    {
        var builder = new NaviConfigBuilder(services);
        config?.Invoke(builder);
        builder.ConfigureServices();
        return services
            .AddNaviServices(builder.ConfigureOptions, credentials)
            .AddScoped<INaviCorrelationIdContext, NaviCorrelationIdContext>()
            .AddScoped(typeof(IProducer<,>), typeof(TypedProducer<,>))
            .AddScoped(typeof(IProducer<,,>), typeof(TypedProducer<,,>))
            .AddSingleton<IConsumerFactory, ConsumerFactory>()
            .AddSingleton<IConsumerJob, ConcurrentConsumerJob>()
            .AddHostedService<NaviHostedService>();
    }

    public static void UseNavi(this IEndpointRouteBuilder app)
    {
        var settings = app.ServiceProvider.GetService<IOptions<NaviConfig>>();
        if (settings is null)
            throw new InvalidOperationException("You should call AddNavi before");

        app.MapTopicEndpoints(settings.Value);
    }
}
