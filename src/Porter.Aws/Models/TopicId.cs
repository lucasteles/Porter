using Porter.Extensions;

namespace Porter.Models;

sealed record TopicId
{
    public TopicId(string topic, PorterTopicNameConfig config)
    {
        if (!IsValidTopicName(topic))
            throw new ArgumentException($"Invalid topic name {topic}", nameof(topic));

        RawName = topic;
        Prefix = config.Prefix.ToSnakeCase();
        Suffix = config.Suffix.ToSnakeCase();
        Event = topic.ToSnakeCase();

        TopicName =
            $"{Prefix.ToPascalCase()}{Event.ToPascalCase()}{Suffix.ToPascalCase()}";

        Source = config.Source.ToSnakeCase();
        QueueName =
            $"{Prefix}_{Source}_{Event}_{Suffix}".TrimUnderscores();
    }

    public string RawName { get; }
    public string TopicName { get; }
    public string QueueName { get; }

    public string Event { get; }
    public string Source { get; }
    public string Prefix { get; }
    public string Suffix { get; }

    public static bool IsValidTopicName(string topic) =>
        topic.Length >= 6
        && char.IsLetter(topic[0])
        && topic.All(c => char.IsLetterOrDigit(c) || c is '_');

    public override string ToString() => TopicName;
}
