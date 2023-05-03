using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Porter.Clients;
using Porter.Hosting.Job;
using Porter.Models;
using Porter.Services;

namespace Porter.Testing;

public static class Extensions
{
    public static IServiceCollection MockPorter(this IServiceCollection services) =>
        services
            .PostConfigure<PorterConfig>(config =>
            {
                config.MessageTimeoutInSeconds = int.MaxValue;
                config.LongPollingWaitInSeconds = 0;
                config.RaiseExceptions = true;
            })
            .RemoveAll<IConsumeDriver>()
            .RemoveAll<IProduceDriver>()
            .RemoveAll<IConsumerJob>()
            .RemoveAll<IPorterResourceManager>()
            .AddSingleton<InMemoryBroker>()
            .AddSingleton<IFakeBroker>(sp => sp.GetRequiredService<InMemoryBroker>())
            .AddSingleton<IConsumeDriver>(sp => sp.GetRequiredService<InMemoryBroker>())
            .AddSingleton<IProduceDriver>(sp => sp.GetRequiredService<InMemoryBroker>())
            .AddSingleton<IConsumerJob>(sp => sp.GetRequiredService<InMemoryBroker>())
            .AddSingleton<IPorterResourceManager>(sp => sp.GetRequiredService<InMemoryBroker>());
}
