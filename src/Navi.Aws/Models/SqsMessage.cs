global using IMessage = Navi.Models.IMessage<string>;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace Navi.Models;

public interface IMessage<out TBody> where TBody : notnull
{
    Guid MessageId { get; }
    Guid? CorrelationId { get; }
    DateTime Datetime { get; }
    TBody Body { get; }
    uint RetryNumber { get; }
    string QueueUrl { get; }
    string? TopicArn { get; }
    Task Delete();
    Task Release(TimeSpan delay);

    IMessage<TMap> Map<TMap>(Func<TBody, TMap> selector) where TMap : notnull;

    string Location();
}

readonly struct SqsMessage<TBody> : IMessage<TBody> where TBody : notnull
{
    readonly string receiptHandle;
    readonly IAmazonSQS sqs;
    public TBody Body { get; }

    public required DateTime Datetime { get; init; }
    public required Guid MessageId { get; init; }
    public required string QueueUrl { get; init; }
    public uint RetryNumber { get; init; } = 0;
    public Guid? CorrelationId { get; init; }
    public string? TopicArn { get; init; }

    internal SqsMessage(in TBody body, string receiptHandle, IAmazonSQS sqs)
    {
        this.receiptHandle = receiptHandle;
        this.sqs = sqs;
        Body = body;
    }

    public async Task Delete() =>
        await sqs.DeleteMessageAsync(
                new()
                {
                    QueueUrl = QueueUrl,
                    ReceiptHandle = receiptHandle,
                },
                CancellationToken.None)
            .ConfigureAwait(false);

    public async Task Release(TimeSpan delay)
    {
        try
        {
            await sqs.ChangeMessageVisibilityAsync(
                    new()
                    {
                        QueueUrl = QueueUrl,
                        ReceiptHandle = receiptHandle,
                        VisibilityTimeout = delay.Seconds,
                    },
                    CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (MessageNotInflightException)
        {
            // ignored
        }
    }

    public IMessage<TMap> Map<TMap>(Func<TBody, TMap> selector) where TMap : notnull =>
        new SqsMessage<TMap>(selector(Body), receiptHandle, sqs)
        {
            Datetime = Datetime,
            CorrelationId = CorrelationId,
            QueueUrl = QueueUrl,
            TopicArn = TopicArn,
            RetryNumber = RetryNumber,
            MessageId = MessageId,
        };

    public string Location() =>
        $"{TopicArn?.Split(":").LastOrDefault() ?? "???"}.{Path.GetFileName(QueueUrl)}";
}

record MessageEnvelope(
    string Event,
    [property: JsonPropertyName("datetime")]
    DateTime DateTime,
    JsonDocument Payload,
    Guid? MessageId = null,
    Guid? CorrelationId = null);
