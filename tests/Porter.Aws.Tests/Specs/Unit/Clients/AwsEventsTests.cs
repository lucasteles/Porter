using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Microsoft.Extensions.Logging;
using Porter.Aws.Tests.TestUtils;
using Porter.Aws.Tests.TestUtils.Fixtures;
using Porter.Clients;

namespace Porter.Aws.Tests.Specs.Unit.Clients;

public class AwsEventsTests : BaseTest
{
    [Test]
    public async Task TopicExistsShouldReturnTrueIfRuleExists()
    {
        var topicName = faker.TopicName(new() { Source = "Source" });
        mocker.Provide(A.Fake<ILogger<AwsEvents>>());
        var aws = mocker.Generate<AwsEvents>();

        ListRulesRequest request = new() { Limit = 100, NamePrefix = topicName.TopicName };

        ListRulesResponse response = new()
        {
            Rules = new List<Rule> { new() { Name = topicName.TopicName, State = RuleState.ENABLED } },
        };

        A.CallTo(() => mocker.Resolve<IAmazonEventBridge>()
                .ListRulesAsync(A<ListRulesRequest>.That.IsEquivalentTo(request), A<CancellationToken>._))
            .Returns(response);

        var result = await aws.RuleExists(topicName, default);

        result.Should().BeTrue();
    }
}
