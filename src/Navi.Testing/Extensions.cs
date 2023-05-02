using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Navi.Clients;
using Navi.Hosting.Job;
using Navi.Models;
using Navi.Services;

namespace Navi.Testing;

public static class Extensions
{
    public static IServiceCollection MockNavi(this IServiceCollection services) =>
        services
            .PostConfigure<NaviConfig>(config =>
            {
                config.MessageTimeoutInSeconds = int.MaxValue;
                config.LongPollingWaitInSeconds = 0;
                config.RaiseExceptions = true;
            })
            .RemoveAll<IConsumeDriver>()
            .RemoveAll<IProduceDriver>()
            .RemoveAll<IConsumerJob>()
            .RemoveAll<INaviResourceManager>()
            .AddSingleton<InMemoryBroker>()
            .AddSingleton<IFakeBroker>(sp => sp.GetRequiredService<InMemoryBroker>())
            .AddSingleton<IConsumeDriver>(sp => sp.GetRequiredService<InMemoryBroker>())
            .AddSingleton<IProduceDriver>(sp => sp.GetRequiredService<InMemoryBroker>())
            .AddSingleton<IConsumerJob>(sp => sp.GetRequiredService<InMemoryBroker>())
            .AddSingleton<INaviResourceManager>(sp => sp.GetRequiredService<InMemoryBroker>());
}
