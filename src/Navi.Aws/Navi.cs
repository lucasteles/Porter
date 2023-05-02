using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Navi.Models;

namespace Navi;

public static class Navi
{
    public static INaviClient CreateClient(
        Action<NaviConfig>? config = null,
        AWSCredentials? credentials = null,
        Action<ILoggingBuilder>? logConfig = null
    ) =>
        new ServiceCollection()
            .AddNaviServices(config, credentials)
            .AddLogging(logConfig ?? delegate { })
            .BuildServiceProvider()
            .CreateScope()
            .ServiceProvider
            .GetRequiredService<INaviClient>();

    public static IProducerClient CreateProducer(
        Action<NaviConfig>? config = null,
        AWSCredentials? credentials = null,
        Action<ILoggingBuilder>? logConfig = null
    ) => CreateClient(config, credentials, logConfig);

    public static IConsumerClient CreateConsumer(
        Action<NaviConfig>? config = null,
        AWSCredentials? credentials = null,
        Action<ILoggingBuilder>? logConfig = null
    ) => CreateClient(config, credentials, logConfig);
}
