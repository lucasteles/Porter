using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Navi.Clients;
using Navi.Extensions;
using Navi.Models;
using Navi.Services;

namespace Navi.Aws.Tests.TestUtils;

static class AwsServiceExtensions
{
    public static async Task<MessageEnvelope[]> GetMessages(this IAmazonSQS sqs, INaviMessageSerializer serializer,
        TopicId topic)
    {
        var messages = await sqs.GetRawMessages(topic);

        return messages
            .Select(x => x.Body.EncodeAsUtf8())
            .Select(x => JsonSerializer.Deserialize<AwsSqs.SnsEnvelope>(x)!)
            .Select(x => serializer.Deserialize<MessageEnvelope>(x.Message)!)
            .ToArray();
    }

    public static async Task<Message[]> GetRawMessages(this IAmazonSQS sqs, TopicId topic)
    {
        var url = (await sqs.GetQueueUrlAsync(topic.QueueName)).QueueUrl;
        var messages = await sqs.ReceiveMessageAsync(url);
        return messages?.Messages.ToArray() ?? Array.Empty<Message>();
    }

    public static async Task<GetQueueAttributesResponse> GetQueueInfo(this IAmazonSQS sqs, string queue)
    {
        var url = (await sqs.GetQueueUrlAsync(queue)).QueueUrl;
        var info = await sqs.GetQueueAttributesAsync(url, new() { QueueAttributeName.All });
        return info;
    }

    public static async Task<(int Total, int Processing)> GetMessageStats(this IAmazonSQS sqs, string queue)
    {
        var info = await sqs.GetQueueInfo(queue);
        return (info.ApproximateNumberOfMessages, info.ApproximateNumberOfMessagesNotVisible);
    }

    public static async Task<bool> HasMessagesOn(this IAmazonSQS sqs, string queue) =>
        await sqs.GetMessageStats(queue) is { Total: > 0 };

    public static async Task<bool> HasMessagesOn(this IAmazonSQS sqs, TopicId topic) =>
        await sqs.GetMessageStats(topic.QueueName) is { Total: > 0 };
}
