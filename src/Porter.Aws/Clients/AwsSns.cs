using System.Text.Json;
using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Microsoft.Extensions.Logging;
using Porter.Models;

namespace Porter.Clients;

sealed class AwsSns
{
    readonly AwsKms kms;
    readonly ILogger<AwsSns> logger;
    readonly IAmazonSimpleNotificationService sns;

    public AwsSns(IAmazonSimpleNotificationService sns, AwsKms kms, ILogger<AwsSns> logger)
    {
        this.sns = sns;
        this.kms = kms;
        this.logger = logger;
    }

    public async Task<SnsArn> EnsureTopic(TopicId topicId, CancellationToken ctx)
    {
        var policy = GetPolicy(topicId.Event, RegionEndpoint.USEast1);
        var keyId = await kms.GetKey(ctx) ??
                    throw new InvalidOperationException("Default KMS EncryptionKey Id not found");

        logger.LogInformation("Ensuring SNS topic: {TopicIdTopicName}", topicId.TopicName);

        try
        {
            CreateTopicRequest request = new()
            {
                Name = topicId.TopicName,
                Attributes = new()
                {
                    [QueueAttributeName.KmsMasterKeyId] = keyId.Value,
                    [QueueAttributeName.Policy] = policy,
                },
            };

            var response = await sns.CreateTopicAsync(request,
                    ctx)
                .ConfigureAwait(false);

            logger.LogDebug("SNS Topic Creation Response is: {Response}",
                response.HttpStatusCode);
            return new(response.TopicArn);
        }
        catch (Exception e) when (e.Message.Contains("Topic already exists",
                                      StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("SNS Topic Creation failure: {Error}", e.Message);
            logger.LogInformation("Trying to get existing topic info");

            CreateTopicRequest request = new()
            {
                Name = topicId.TopicName,
            };
            var response = await sns.CreateTopicAsync(request,
                    ctx)
                .ConfigureAwait(false);

            logger.LogDebug("SNS Topic Creation Response is: {Response}",
                response.HttpStatusCode);

            return new(response.TopicArn);
        }
    }

    public async Task Subscribe(SnsArn snsArn, SqsArn sqsArn, CancellationToken ctx)
    {
        logger.LogInformation("Subscribing SQS[{SqsArn}] to SNS[[{SnsArnValue}]", sqsArn,
            snsArn.Value);

        var subscriptions = await sns
            .ListSubscriptionsByTopicAsync(new ListSubscriptionsByTopicRequest
            {
                TopicArn = snsArn.Value,
            }, ctx)
            .ConfigureAwait(false);

        if (subscriptions.Subscriptions.Exists(x => x.Endpoint == sqsArn.Value))
        {
            logger.LogInformation("Subscription for {SqsArnValue} already added to {SnsArnValue}",
                sqsArn.Value, snsArn.Value);
            return;
        }

        await sns.SubscribeAsync(
                new SubscribeRequest
                {
                    TopicArn = snsArn.Value,
                    Protocol = "sqs",
                    Endpoint = sqsArn.Value,
                }, ctx)
            .ConfigureAwait(false);
    }

    static string GetPolicy(string resourceName, RegionEndpoint region) =>
        JsonSerializer.Serialize(
            new
            {
                Version = "2008-10-17",
                Id = "__default_policy_ID",
                Statement = new object[]
                {
                    new
                    {
                        Sid = "__default_statement_ID",
                        Effect = "Allow",
                        Principal = new
                        {
                            AWS = "*",
                        },
                        Action = new[]
                        {
                            "SNS:GetTopicAttributes",
                            "SNS:SetTopicAttributes",
                            "SNS:AddPermission",
                            "SNS:RemovePermission",
                            "SNS:DeleteTopic",
                            "SNS:Subscribe",
                            "SNS:ListSubscriptionsByTopic",
                            "SNS:Publish",
                            "SNS:Receive",
                        },
                        Resource = $"arn:aws:sns:{region.SystemName}:*:{resourceName}",
                    },
                    new
                    {
                        Sid = "Enable Eventbridge Events",
                        Effect = "Allow",
                        Principal = new
                        {
                            Service = "events.amazonaws.com",
                        },
                        Action = "sns:Publish",
                        Resource = "*",
                    },
                },
            });
}
