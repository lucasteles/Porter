using Amazon.Runtime;

namespace Navi.Aws.Tests.Specs.Unit;

public class NaviTests
{
    [Test]
    public void ShouldInstantiateClient()
    {
        var client = Navi.CreateClient(c =>
        {
            c.Source = "app";
        }, new AnonymousAWSCredentials());

        client.Should().NotBeNull();
    }

    [Test]
    public void ShouldInstantiateConsumer()
    {
        var client = Navi.CreateConsumer(c =>
        {
            c.Source = "app";
        }, new AnonymousAWSCredentials());

        client.Should().NotBeNull();
    }

    [Test]
    public void ShouldInstantiateProducer()
    {
        var client = Navi.CreateProducer(c =>
        {
            c.Source = "app";
        }, new AnonymousAWSCredentials());

        client.Should().NotBeNull();
    }
}
