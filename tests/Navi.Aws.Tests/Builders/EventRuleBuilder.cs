using System.Text.RegularExpressions;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Bogus;
using Navi.Aws.Tests.TestUtils;
using Navi.Models;

namespace Navi.Aws.Tests.Builders;

public class EventRuleBuilder
{
    readonly Faker faker = new("pt_BR");

    string state = RuleState.ENABLED;

    public EventRuleBuilder(NaviTopicNameConfig config)
    {
        EventName = faker.TopicNameString();
        TopicName = EventName
            .Split("_")
            .Select(x => x.ToTitleCase())
            .Concat();

        Topic = new(EventName, config);
    }

    internal TopicId Topic { get; }
    public string TopicName { get; }
    public string EventName { get; }

    public string EventPattern => $@"
{{
  ""detail-type"": [""{EventName}""],
  ""detail"": {{
    ""event"": [""{EventName}""]
  }}
}}";

    public EventRuleBuilder Disabled()
    {
        state = RuleState.DISABLED;
        return this;
    }

    public PutRuleRequest CreateRule() => new()
    {
        Name = Topic.TopicName,
        Description = faker.Lorem.Paragraph(),
        State = state,
        EventBusName = "default",
        EventPattern = Regex.Replace(EventPattern, @"\r\n?|\n", string.Empty),
    };
}
