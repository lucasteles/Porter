using Newtonsoft.Json.Linq;

namespace Navi.Testing;

public static class AsyncExtensions
{
    public static async Task HaveAnyMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, string because = "", params object[] becauseArgs)
        => (await @this).ContainsAnyMessage(topicName, because, becauseArgs);

    public static async Task Topic(
        this Task<FakeBrokerAssertions> @this,
        string topicName)
        => (await @this).Topic(topicName);

    public static async Task ContainMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, object message,
        string because = "", params object[] becauseArgs)
        => (await @this).ContainsMessage(topicName, message, because, becauseArgs);

    public static async Task ContainMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, string message,
        string because = "", params object[] becauseArgs)
        => (await @this).ContainsMessage(topicName, message, because, becauseArgs);

    public static async Task ContainMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, JToken message,
        string because = "", params object[] becauseArgs)
        => (await @this).ContainsMessage(topicName, message, because, becauseArgs);

    public static async Task NotContainMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, object message,
        string because = "", params object[] becauseArgs)
        => (await @this).NotContainsMessage(topicName, message, because, becauseArgs);

    public static async Task NotContainMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, string message,
        string because = "", params object[] becauseArgs)
        => (await @this).NotContainsMessage(topicName, message, because, becauseArgs);

    public static async Task NotContainMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, JToken message,
        string because = "", params object[] becauseArgs)
        => (await @this).NotContainsMessage(topicName, message, because, becauseArgs);

    public static async Task ContainsEquivalentMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, JToken message,
        string because = "", params object[] becauseArgs)
        => (await @this).ContainsMessageEquivalentTo(topicName, message, because, becauseArgs);

    public static async Task ContainsEquivalentMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, object message,
        string because = "", params object[] becauseArgs)
        => (await @this).ContainsMessageEquivalentTo(topicName, message, because, becauseArgs);

    public static async Task ContainsEquivalentMessage(
        this Task<FakeBrokerAssertions> @this,
        string topicName, string message,
        string because = "", params object[] becauseArgs)
        => (await @this).ContainsMessageEquivalentTo(topicName, message, because, becauseArgs);

    public static async Task MessagesBe(
        this Task<FakeBrokerAssertions> @this,
        object topicAndMessages,
        string because = "", params string[] becauseArgs)
        => (await @this).AllMessagesBe(topicAndMessages, because, becauseArgs);

    public static async Task MessagesBe(
        this Task<FakeBrokerAssertions> @this,
        Dictionary<string, object[]> messages,
        string because = "", params string[] becauseArgs)
        => (await @this).AllMessagesBe(messages, because, becauseArgs);

    public static async Task NotMessagesBe(
        this Task<FakeBrokerAssertions> @this,
        Dictionary<string, object[]> messages,
        string because = "", params string[] becauseArgs)
        => (await @this).AllMessagesNotBe(messages, because, becauseArgs);

    public static async Task NotMessagesBe(
        this Task<FakeBrokerAssertions> @this,
        object topicAndMessages,
        string because = "", params string[] becauseArgs)
        => (await @this).AllMessagesNotBe(topicAndMessages, because, becauseArgs);

    public static async Task JsonMessagesBe(
        this Task<FakeBrokerAssertions> @this,
        Dictionary<string, string[]> messages,
        string because = "",
        params string[] becauseArgs)
        => (await @this).AllJsonMessagesBe(messages, because, becauseArgs);

    public static async Task NotJsonMessagesBe(
        this Task<FakeBrokerAssertions> @this,
        Dictionary<string, string[]> messages,
        string because = "",
        params string[] becauseArgs)
        => (await @this).AllJsonMessagesNotBe(messages, because, becauseArgs);

    public static async Task BeMessagesEquivalent(
        this Task<FakeBrokerAssertions> @this,
        object topicAndMessages,
        string because = "", params string[] becauseArgs)
        => (await @this).AllMessageBeEquivalentTo(topicAndMessages, because, becauseArgs);

    public static async Task BeMessagesEquivalent(
        this Task<FakeBrokerAssertions> @this,
        Dictionary<string, object[]> messages,
        string because = "", params string[] becauseArgs)
        => (await @this).AllMessageBeEquivalentTo(messages, because, becauseArgs);

    public static async Task BeMessagesJsonEqual(
        this Task<FakeBrokerAssertions> @this,
        Dictionary<string, string[]> messages,
        string because = "",
        params string[] becauseArgs)
        => (await @this).AllJsonMessagesBeEquivalentTo(messages, because, becauseArgs);
}
