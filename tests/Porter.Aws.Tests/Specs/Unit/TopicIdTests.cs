using Porter.Models;

namespace Porter.Aws.Tests.Specs.Unit;

public class TopicIdTests
{
    public static readonly PorterTopicNameConfig EmptyConfig =
        new() { Prefix = string.Empty, Suffix = string.Empty, Source = "source" };

    [TestCase("0name")]
    [TestCase("name@bad")]
    [TestCase("name$bad")]
    [TestCase("")]
    [TestCase("a")]
    [TestCase("ab")]
    [TestCase("abcde")]
    public void ShouldThrowIfBadName(string badName)
    {
        var action = () => new TopicId(badName, EmptyConfig);
        action.Should().Throw<ArgumentException>();
    }

    [TestCase("good_name")]
    [TestCase("abcdef")]
    [TestCase("abc123")]
    public void ShouldNotThrowIfGoodName(string goodName)
    {
        var action = () => new TopicId(goodName, EmptyConfig);
        action.Should().NotThrow();
    }

    [Test]
    public void ShouldNormalizeName()
    {
        const string name = "NameToNormalize";
        const string expected = "name_to_normalize";
        var topic = new TopicId(name, EmptyConfig);

        topic.Event.Should().Be(expected);
    }

    [Test]
    public void ShouldNormalizeNameWithPrefixAndSufix()
    {
        const string prefix = "ThePrefix";
        const string sufix = "TheSufix";
        const string name = "NameToNormalize";
        const string expected = "ThePrefixNameToNormalizeTheSufix";
        var topic = new TopicId(name, new PorterTopicNameConfig { Prefix = prefix, Suffix = sufix, Source = "source" });

        topic.TopicName.Should().Be(expected);
    }

    [Test]
    public void ShouldNormalizeQueueName()
    {
        const string prefix = "ThePrefix";
        const string suffix = "TheSuffix";
        const string source = "TheSource";
        const string name = "NameToNormalize";
        const string expected = "the_prefix_the_source_name_to_normalize_the_suffix";

        var topic = new TopicId(name, new PorterTopicNameConfig { Prefix = prefix, Suffix = suffix, Source = source });

        topic.QueueName.Should().Be(expected);
    }

    [TestCase("ThePrefix", "")]
    [TestCase("", "TheSuffix")]
    [TestCase("", "")]
    public void ShouldNotHaveUnderscoreAtEdges(string prefix, string suffix)
    {
        const string source = "TheSource";
        const string name = "NameToNormalize";

        var topic = new TopicId(name, new PorterTopicNameConfig { Prefix = "", Suffix = suffix, Source = source });

        topic.QueueName.Should().NotStartWith("_").And.NotEndWith("_");
    }
}
