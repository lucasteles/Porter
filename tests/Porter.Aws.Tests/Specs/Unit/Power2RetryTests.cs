using Porter.Services;

namespace Porter.Aws.Tests.Specs.Unit;

public class Power2RetryTests
{
    [TestCase(0u, 1)]
    [TestCase(1u, 2)]
    [TestCase(2u, 4)]
    [TestCase(3u, 8)]
    [TestCase(4u, 16)]
    [TestCase(5u, 32)]
    public void ShouldReturnCorrectValue(uint retry, int expectedSeconds)
    {
        var expected = TimeSpan.FromSeconds(expectedSeconds);
        var sut = new Power2RetryStrategy();
        sut.Evaluate(retry).Should().Be(expected);
    }
}
