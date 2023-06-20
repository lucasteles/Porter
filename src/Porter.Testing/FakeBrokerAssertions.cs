using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Json;
using FluentAssertions.Primitives;
using Newtonsoft.Json.Linq;
using Porter.Services;

namespace Porter.Testing;

public class
    FakeBrokerAssertions : ReferenceTypeAssertions<IFakeReadonlyBroker, FakeBrokerAssertions>
{
    public FakeBrokerAssertions(IFakeReadonlyBroker subject) : base(subject)
    {
    }

    protected override string Identifier => "fakebroker";

    public AndConstraint<FakeBrokerAssertions> ContainsAnyMessage(
        string topicName, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(!string.IsNullOrEmpty(topicName))
            .FailWith(
                "You can't assert a message was produced if you don't pass a proper topic name")
            .Then
            .Given(() => Subject.ProducedOn(topicName))
            .ForCondition(messages => messages.Length > 0)
            .FailWith("Expected {0} to contain messages{reason}, but not found any.",
                _ => topicName);

        return new AndConstraint<FakeBrokerAssertions>(this);
    }

    internal AndConstraint<JTokenAssertions> Topic(
        string topicName)
    {
        var messages = JArray.Parse($"[{string.Join(",", Subject.ProducedOn(topicName))}]");
        return new AndConstraint<JTokenAssertions>(new(messages));
    }

    #region ContainsMessage

    AndConstraint<FakeBrokerAssertions> ContainsMessageAssert(
        string topicName, JToken message, bool not,
        string because = "", params object[] becauseArgs)
    {
        var itemOnArray = new JArray(message);
        if (not)
            Topic(topicName).And.NotBeEquivalentTo(itemOnArray, because, becauseArgs);
        else
            Topic(topicName).And.BeEquivalentTo(itemOnArray, because, becauseArgs);

        return new AndConstraint<FakeBrokerAssertions>(this);
    }

    public AndConstraint<FakeBrokerAssertions> ContainsMessage(
        string topicName, object message,
        string because = "", params object[] becauseArgs)
    {
        var jsonMessage = Subject.Serializer.Serialize(message);
        return ContainsMessage(topicName, jsonMessage, because, becauseArgs);
    }

    public AndConstraint<FakeBrokerAssertions> ContainsMessage(
        string topicName, string message,
        string because = "", params object[] becauseArgs)
    {
        var jsonMessage = JToken.Parse(message);
        return ContainsMessage(topicName, jsonMessage, because, becauseArgs);
    }

    public AndConstraint<FakeBrokerAssertions> ContainsMessage(
        string topicName, JToken message,
        string because = "", params object[] becauseArgs) =>
        ContainsMessageAssert(topicName, message, not: false, because, becauseArgs);

    public AndConstraint<FakeBrokerAssertions> NotContainsMessage(
        string topicName, object message,
        string because = "", params object[] becauseArgs)
    {
        var jsonMessage = Subject.Serializer.Serialize(message);
        return NotContainsMessage(topicName, jsonMessage, because, becauseArgs);
    }

    public AndConstraint<FakeBrokerAssertions> NotContainsMessage(
        string topicName, string message,
        string because = "", params object[] becauseArgs)
    {
        var jsonMessage = JToken.Parse(message);
        return NotContainsMessage(topicName, jsonMessage, because, becauseArgs);
    }

    public AndConstraint<FakeBrokerAssertions> NotContainsMessage(
        string topicName, JToken message,
        string because = "", params object[] becauseArgs) =>
        ContainsMessageAssert(topicName, message, not: true, because, becauseArgs);

    #endregion

    #region ContainsEquivalentMessageTo

    public AndConstraint<FakeBrokerAssertions> ContainsMessageEquivalentTo(
        string topicName, JToken message,
        string because = "", params object[] becauseArgs)
    {
        var itemOnArray = new JArray(message);
        Topic(topicName).And.ContainSubtree(itemOnArray, because, becauseArgs);
        return new AndConstraint<FakeBrokerAssertions>(this);
    }

    public AndConstraint<FakeBrokerAssertions> ContainsMessageEquivalentTo(
        string topicName, object message,
        string because = "", params object[] becauseArgs)
    {
        var jsonMessage = Subject.Serializer.Serialize(message);
        return ContainsMessageEquivalentTo(topicName, jsonMessage, because, becauseArgs);
    }

    public AndConstraint<FakeBrokerAssertions> ContainsMessageEquivalentTo(
        string topicName, string message,
        string because = "", params object[] becauseArgs)
    {
        var jsonMessage = JToken.Parse(message);
        return ContainsMessageEquivalentTo(topicName, jsonMessage, because, becauseArgs);
    }

    #endregion

    #region AllMessagesBe

    AndConstraint<FakeBrokerAssertions> AllMessagesBeAssert(
        object topicAndMessages, bool not,
        string because = "", params string[] becauseArgs)
    {
        var expected = JToken.Parse(Subject.Serializer.Serialize(topicAndMessages));
        var pushedMessages = Subject.ProducedMessages();

        var received = DictToJToken(pushedMessages);

        if (not)
            received.Should().NotBeEquivalentTo(expected, because, becauseArgs);
        else
            received.Should().BeEquivalentTo(expected, because, becauseArgs);

        return new AndConstraint<FakeBrokerAssertions>(this);
    }

    public AndConstraint<FakeBrokerAssertions> AllMessagesBe(
        object topicAndMessages,
        string because = "", params string[] becauseArgs) =>
        AllMessagesBeAssert(topicAndMessages, not: false, because, becauseArgs);

    public AndConstraint<FakeBrokerAssertions> AllMessagesBe(
        Dictionary<string, object[]> messages,
        string because = "", params string[] becauseArgs)
    {
        var strMessages = messages.ToDictionary(
            x => x.Key,
            x => x.Value.Select(v => Subject.Serializer.Serialize(v)).ToArray());

        return AllJsonMessagesBe(strMessages, because, becauseArgs);
    }

    public AndConstraint<FakeBrokerAssertions> AllMessagesNotBe(
        Dictionary<string, object[]> messages,
        string because = "", params string[] becauseArgs)
    {
        var strMessages = messages.ToDictionary(
            x => x.Key,
            x => x.Value.Select(v => Subject.Serializer.Serialize(v)).ToArray());

        return AllJsonMessagesNotBe(strMessages, because, becauseArgs);
    }

    public AndConstraint<FakeBrokerAssertions> AllMessagesNotBe(
        object topicAndMessages,
        string because = "", params string[] becauseArgs) =>
        AllMessagesBeAssert(topicAndMessages, not: true, because, becauseArgs);

    AndConstraint<FakeBrokerAssertions> JsonAllMessagesBeAssertion(
        Dictionary<string, string[]> messages, bool not,
        string because = "",
        params string[] becauseArgs)
    {
        var pushedMessages = Subject.ProducedMessages();
        var expected = DictToJToken(messages);
        var received = DictToJToken(pushedMessages);

        if (not)
            received.Should().NotBeEquivalentTo(expected, because, becauseArgs);
        else
            received.Should().BeEquivalentTo(expected, because, becauseArgs);

        return new AndConstraint<FakeBrokerAssertions>(this);
    }

    public AndConstraint<FakeBrokerAssertions> AllJsonMessagesBe(
        Dictionary<string, string[]> messages,
        string because = "",
        params string[] becauseArgs) =>
        JsonAllMessagesBeAssertion(messages, not: false, because, becauseArgs);

    public AndConstraint<FakeBrokerAssertions> AllJsonMessagesNotBe(
        Dictionary<string, string[]> messages,
        string because = "",
        params string[] becauseArgs) =>
        JsonAllMessagesBeAssertion(messages, not: true, because, becauseArgs);

    #endregion

    #region AllMessageBeEquivalentTo

    public AndConstraint<FakeBrokerAssertions> AllMessageBeEquivalentTo(
        object topicAndMessages,
        string because = "", params string[] becauseArgs)
    {
        var expected = JToken.Parse(Subject.Serializer.Serialize(topicAndMessages));
        var pushedMessages = Subject.ProducedMessages();

        var received = DictToJToken(pushedMessages);

        received.Should().ContainSubtree(expected, because, becauseArgs);
        return new AndConstraint<FakeBrokerAssertions>(this);
    }

    public AndConstraint<FakeBrokerAssertions> AllMessageBeEquivalentTo(
        Dictionary<string, object[]> messages,
        string because = "", params string[] becauseArgs)
    {
        var strMessages = messages.ToDictionary(
            x => x.Key,
            x => x.Value.Select(v => Subject.Serializer.Serialize(v)).ToArray());

        return AllJsonMessagesBeEquivalentTo(strMessages, because, becauseArgs);
    }

    public AndConstraint<FakeBrokerAssertions> AllJsonMessagesBeEquivalentTo(
        Dictionary<string, string[]> messages,
        string because = "",
        params string[] becauseArgs)
    {
        var pushedMessages = Subject.ProducedMessages();
        var expected = DictToJToken(messages);
        var received = DictToJToken(pushedMessages);

        received.Should().ContainSubtree(expected, because, becauseArgs);

        return new AndConstraint<FakeBrokerAssertions>(this);
    }

    #endregion

    #region HaveBeenConsumedBy

    public AndConstraint<FakeBrokerAssertions> HaveBeenConsumedBy(
        string topicName, Type consumer, string? message = null,
        string because = "", params object[] becauseArgs)
    {
        var consumed = Subject.GetConsumed(consumer, topicName);

        consumed.Should().NotBeEmpty(
            $"Expected {consumer} to have consumed a message, but found none. {because}",
            becauseArgs);

        if (message is null or "")
            return new AndConstraint<FakeBrokerAssertions>(this);

        var messageJson = JToken.Parse(message);
        var consumedMessages = consumed.Select(JToken.Parse).ToArray();
        consumedMessages.Should().ContainEquivalentOf(messageJson, because, becauseArgs);

        return new AndConstraint<FakeBrokerAssertions>(this);
    }

    public AndConstraint<FakeBrokerAssertions> HaveBeenConsumedBy<TConsumer>(
        string topicName, object? message = null,
        string because = "", params object[] becauseArgs) where TConsumer : IWeakConsumer
    {
        var stringMessage = (message ?? string.Empty) as string ??
                            Subject.Serializer.Serialize(message);

        return HaveBeenConsumedBy(topicName, typeof(TConsumer), stringMessage, because,
            becauseArgs);
    }

    #endregion

    static JToken DictToJToken(IReadOnlyDictionary<string, string[]> values)
    {
        var root = new JObject();
        foreach (var (key, value) in values)
        {
            var items = new JArray(value.Select(JToken.Parse).ToArray());
            root.Add(key, items);
        }

        return root;
    }

    public async Task<FakeBrokerAssertions> When(Func<Task> action)
    {
        if (Subject is not IFakeBroker fake)
            throw new InvalidOperationException();

        var messages = await fake.Delta(action);

        var consumed = fake.GetConsumed()
            .SelectMany(c => c.Value,
                (item, m) => (item.Key, m.Consumer, m.Message))
            .GroupBy(x => (x.Consumer, x.Key))
            .ToDictionary(
                x => x.Key,
                x => x.Select(m => m.Message).ToArray());

        var broker = new DeltaFakerBroker(messages, consumed, fake.Serializer);
        return new FakeBrokerAssertions(broker);
    }
}

class DeltaFakerBroker : IFakeReadonlyBroker
{
    readonly IReadOnlyDictionary<string, string[]> messages;
    readonly IReadOnlyDictionary<(Type, string), string[]> consumed;

    public DeltaFakerBroker(
        IReadOnlyDictionary<string, string[]> messages,
        IReadOnlyDictionary<(Type, string), string[]> consumed,
        IPorterMessageSerializer serializer)
    {
        this.messages = messages;
        this.consumed = consumed;
        Serializer = serializer;
    }

    public IReadOnlyDictionary<string, string[]> ProducedMessages() => messages;
    public IPorterMessageSerializer Serializer { get; }
    public string[] GetConsumed(Type consumer, string topic) => consumed[(consumer, topic)];

    public string[] ProducedOn(string topic) => messages[topic];
}

public static class FakeBrokerAssertionsExtensions
{
    public static FakeBrokerAssertions Should(this IFakeBroker instance) => new(instance);
}
