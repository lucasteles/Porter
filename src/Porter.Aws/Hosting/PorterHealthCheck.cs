using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net;
using Amazon.Runtime;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Porter.Hosting.Config;
using Porter.Models;

namespace Porter.Hosting;

public class PorterHealthCheck : IHealthCheck
{
    readonly IAmazonSQS sqs;
    readonly ImmutableArray<(TopicId Id, bool HasConsumer)> topics;
    static readonly ConcurrentDictionary<TopicId, string> queueUrls = new();

    public PorterHealthCheck(
        IOptions<PorterConfig> settings,
        IServiceProvider provider,
        IAmazonSQS sqs
    )
    {
        this.sqs = sqs;

        this.topics = provider.GetRequiredService<IEnumerable<ITopicConfigurationBuilder>>()
            .Select(c => (new TopicId(c.TopicName, settings.Value), c.HasConsumer))
            .ToImmutableArray();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await GetQueueStatus(cancellationToken))
                return HealthCheckResult.Unhealthy();

            return HealthCheckResult.Healthy();
        }
        catch
        {
            return HealthCheckResult.Unhealthy();
        }
    }

    async Task<bool> GetQueueStatus(CancellationToken ct)
    {
        var queues = topics
            .Where(x => x.HasConsumer)
            .Select(t => IsQueueAccessible(t.Id, ct));

        var healthyQueues = await Task.WhenAll(queues);
        return Array.TrueForAll(healthyQueues, x => x);
    }

    static bool IsSuccess(AmazonWebServiceResponse response) =>
        response.HttpStatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent;

    async Task<bool> IsQueueAccessible(TopicId topic, CancellationToken ct)
    {
        try
        {
            if (!queueUrls.TryGetValue(topic, out var queueUrl))
            {
                var queueUrlResponse = await sqs.GetQueueUrlAsync(topic.QueueName, ct);
                if (!IsSuccess(queueUrlResponse) || queueUrlResponse.QueueUrl is null)
                    return false;
                queueUrl = queueUrlResponse.QueueUrl;
                queueUrls.AddOrUpdate(topic, queueUrl, (key, old) => queueUrl);
            }

            var attributesResponse = await sqs.GetQueueAttributesAsync(queueUrl, new(), ct);
            return IsSuccess(attributesResponse);
        }
        catch
        {
            return false;
        }
    }
}
