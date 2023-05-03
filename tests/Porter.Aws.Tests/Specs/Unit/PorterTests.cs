using Amazon.Runtime;

namespace Porter.Aws.Tests.Specs.Unit;

public class PorterTests
{
    [Test]
    public void ShouldInstantiateClient()
    {
        var client = Porter.CreateClient(c =>
        {
            c.Source = "app";
        }, new AnonymousAWSCredentials());

        client.Should().NotBeNull();
    }

    [Test]
    public void ShouldInstantiateConsumer()
    {
        var client = Porter.CreateConsumer(c =>
        {
            c.Source = "app";
        }, new AnonymousAWSCredentials());

        client.Should().NotBeNull();
    }

    [Test]
    public void ShouldInstantiateProducer()
    {
        var client = Porter.CreateProducer(c =>
        {
            c.Source = "app";
        }, new AnonymousAWSCredentials());

        client.Should().NotBeNull();
    }
}
