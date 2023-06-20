using Porter.Services;

namespace Porter.Aws.Tests.Specs.Unit;

public class Power2RetryTests
{
    [TestCase(0, 1)]
    [TestCase(1, 2)]
    [TestCase(2, 4)]
    [TestCase(3, 8)]
    [TestCase(4, 16)]
    [TestCase(5, 32)]
    public void ShouldReturnCorrectValue(int retry, int expectedSeconds)
    {
        var expected = TimeSpan.FromSeconds(expectedSeconds);
        var sut = new Power2RetryStrategy();
        sut.Evaluate(retry).Should().Be(expected);
    }
}
