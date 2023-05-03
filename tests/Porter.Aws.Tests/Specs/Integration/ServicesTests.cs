using Porter;
using Porter.Aws.Tests.TestUtils.Fixtures;
using Porter.Models;
using Porter.Services;

namespace Porter.Aws.Tests.Specs.Integration;

public class ServicesTests : ServicesFixture
{
    [Test]
    public void ShouldRegisterPublicServices()
    {
        var producer = GetService<IProducerClient>();
        var consumer = GetService<IConsumerClient>();
        var client = GetService<IPorterClient>();
        new object[]
            {
                producer, consumer, client,
            }
            .Should().NotContainNulls();
    }

    [Test]
    public void ShouldConsumerShouldBeSameSubClient()
    {
        var client = GetService<IPorterClient>();
        var consumer = GetService<IConsumerClient>();
        consumer.Should().BeOfType(client.GetType());
    }

    [Test]
    public void ShouldProducerShouldBeSameSubClient()
    {
        var client = GetService<IPorterClient>();
        var producer = GetService<IProducerClient>();
        producer.Should().BeOfType(client.GetType());
    }

    [Test]
    public void ShouldSerializeRawMessage()
    {
        var message =
            """
            {"event":"lead_analysis_completed","datetime":"2022-11-25T00:46:28.328508+00:00","payload":{"tax_id":"23519244000181","request_id":951,"status":"SUCCESS"}}
            """;

        var response = GetService<IPorterMessageSerializer>().Deserialize<MessageEnvelope>(message);

        response.Payload.RootElement.GetRawText().Should().BeEquivalentTo(
            """
            {"tax_id":"23519244000181","request_id":951,"status":"SUCCESS"}
            """);
    }
}
