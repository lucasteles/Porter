using System.Reflection;
using System.Text.Json;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Porter.Extensions;
using Porter.Models;
using Porter.Services;

namespace Porter.Clients;

interface IProduceDriver : IDisposable
{
    Task<PublishResult> Produce(TopicId topic, string message, Guid? correlationId,
        CancellationToken ct);
}

sealed class AwsEvents : IProduceDriver
{
    readonly PorterConfig config;
    readonly IAmazonEventBridge eventBridge;
    readonly IPorterMessageSerializer serializer;
    readonly IDiagnostics diagnostics;
    readonly ILogger<AwsEvents> logger;
    readonly TagsService tags;
    readonly IPorterClock clock;

    public AwsEvents(
        IAmazonEventBridge eventBridge,
        IPorterMessageSerializer serializer,
        IDiagnostics diagnostics,
        ILogger<AwsEvents> logger,
        IOptions<PorterConfig> config,
        TagsService tags,
        IPorterClock clock)
    {
        this.eventBridge = eventBridge;
        this.serializer = serializer;
        this.diagnostics = diagnostics;
        this.logger = logger;
        this.tags = tags;
        this.clock = clock;
        this.config = config.Value;
    }

    public async Task<bool> RuleExists(TopicId topicId, CancellationToken ct)
    {
        var rules = await eventBridge.ListRulesAsync(new()
        {
            Limit = 100,
            NamePrefix = topicId.TopicName,
        }, ct);

        return rules is not null &&
               rules.Rules.Exists(r =>
                   r.Name.Trim() == topicId.TopicName && r.State == RuleState.ENABLED);
    }

    public async Task PutTarget(TopicId topic, SnsArn snsArn, CancellationToken ct)
    {
        logger.LogInformation("Putting EventBridge SNS target {TopicTopicName}[{SnsArnValue}]", topic.TopicName, snsArn.Value);

        var ruleTargets = await eventBridge.ListTargetsByRuleAsync(new()
        {
            Rule = topic.TopicName,
        }, ct);

        if (ruleTargets.Targets.Exists(x => x.Arn == snsArn.Value))
        {
            logger.LogInformation("Target with {TopicTopicName}[{SnsArnValue}] already added", topic.TopicName, snsArn.Value);
            return;
        }

        var result = await eventBridge
            .PutTargetsAsync(
                new()
                {
                    Rule = topic.TopicName,
                    Targets = new List<Target>
                    {
                        new()
                        {
                            Id = topic.TopicName,
                            Arn = snsArn.Value,
                            InputPath = "$.detail",
                        },
                    },
                }, ct);

        logger.LogInformation("Completed({ResultHttpStatusCode}): EventBridge SNS target {TopicTopicName}[{SnsArnValue}]", result.HttpStatusCode, topic.TopicName, snsArn.Value);
    }

    public async Task<RuleArn> CreateRule(TopicId topicId, CancellationToken ct)
    {
        var eventPattern =
            $@"{{ ""detail-type"": [""{topicId.Event}""], ""detail"": {{ ""event"": [""{topicId.Event}""] }} }}";

        PutRuleRequest request = new()
        {
            Name = topicId.TopicName,
            Tags = tags.GetTags(x => new Tag
            {
                Key = x.Key,
                Value = x.Value,
            }),
            Description =
                $"Created in {Assembly.GetExecutingAssembly().GetName().Name} for {topicId.TopicName} events",
            State = RuleState.ENABLED,
            EventBusName = "default",
            EventPattern = eventPattern,
        };

        logger.LogInformation("Creating EventBridge rule: {TopicIdTopicName}", topicId.TopicName);
        var response = await eventBridge.PutRuleAsync(request, ct).ConfigureAwait(false);
        logger.LogDebug("Event Create/Update Response is: {Response}",
            response.HttpStatusCode);

        return new(response.RuleArn);
    }

    public async Task<PublishResult> Produce(TopicId topic, string message, Guid? correlationId,
        CancellationToken ct)
    {
        var messageId = NewId.NextGuid();

        MessageEnvelope envelope = new(
            topic.Event,
            DateTime: clock.Now(),
            Payload: JsonDocument.Parse(message),
            MessageId: messageId,
            CorrelationId: correlationId
        );

        var body = serializer.Serialize(envelope).EncodeAsUtf8();
        logger.LogDebug("Produce {TopicTopicName}: {Body}", topic.TopicName, body);

        using var activity = diagnostics.StartProducerActivity(topic.TopicName);
        diagnostics.SetActivityMessageAttributes(
            activity, eventBridge.Config.ServiceURL, messageId, correlationId, body);

        PutEventsRequest request = new()
        {
            Entries = new()
            {
                new()
                {
                    DetailType = topic.Event,
                    Source = config.Source,
                    Detail = body,
                },
            },
        };
        var response = await eventBridge.PutEventsAsync(request, ct);

        if (response.FailedEntryCount > 0)
            throw new PorterException(string.Join(",",
                response.Entries.Select(x => x.ErrorMessage)));
        logger.LogDebug("{Topic}: Message produced on {TopicEvent} - {ResponseHttpStatusCode}",
            topic, topic.Event, response.HttpStatusCode);
        diagnostics.AddProducedMessagesCounter(1, topic.RawName);
        return new(response.FailedEntryCount is 0, messageId, correlationId);
    }

    public void Dispose() => eventBridge.Dispose();
}
