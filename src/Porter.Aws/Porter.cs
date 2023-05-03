using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Porter.Models;

namespace Porter;

public static class Porter
{
    public static IPorterClient CreateClient(
        Action<PorterConfig>? config = null,
        AWSCredentials? credentials = null,
        Action<ILoggingBuilder>? logConfig = null
    ) =>
        new ServiceCollection()
            .AddPorterServices(config, credentials)
            .AddLogging(logConfig ?? delegate { })
            .BuildServiceProvider()
            .CreateScope()
            .ServiceProvider
            .GetRequiredService<IPorterClient>();

    public static IProducerClient CreateProducer(
        Action<PorterConfig>? config = null,
        AWSCredentials? credentials = null,
        Action<ILoggingBuilder>? logConfig = null
    ) => CreateClient(config, credentials, logConfig);

    public static IConsumerClient CreateConsumer(
        Action<PorterConfig>? config = null,
        AWSCredentials? credentials = null,
        Action<ILoggingBuilder>? logConfig = null
    ) => CreateClient(config, credentials, logConfig);
}
