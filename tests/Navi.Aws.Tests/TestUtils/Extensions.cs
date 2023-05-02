using System.Globalization;
using System.Text.Json;
using Bogus;
using FakeItEasy.Configuration;
using FluentAssertions.Json;
using Microsoft.Extensions.Logging;
using Navi.Models;
using Newtonsoft.Json.Linq;

namespace Navi.Aws.Tests.TestUtils;

public static class Extensions
{
    public static string OnlyLetterOrDigit(this string str) =>
        string.Concat(str.Where(char.IsLetterOrDigit));

    public static void ShouldBeJsonEquivalent(this string str, object expected) =>
        str.AsJToken().Should().BeEquivalentTo(expected is string s
            ? s.AsJToken()
            : JsonSerializer.Serialize(expected).AsJToken());

    public static void ShouldMessageBodyBeEquivalentTo(this IMessage<string> message,
        object expected) =>
        message.Body.ShouldBeJsonEquivalent(expected);

    public static void ShouldMessagesBeEquivalentTo(this IEnumerable<IMessage<string>> message,
        IEnumerable<object> expected) => JsonSerializer
        .Serialize(message.Select(m => m.Map(e => JsonDocument.Parse(e)))).AsJToken()
        .Should().ContainSubtree(
            JsonSerializer
                .Serialize(expected)
                .AsJToken());

    public static JToken AsJToken(this string json) => JToken.Parse(json);
    public static JsonDocument ToJsonDocument(this string json) => JsonDocument.Parse(json);

    public static string ToJsonString(this JsonDocument json) => JsonSerializer.Serialize(json,
        new JsonSerializerOptions
        {
            WriteIndented = false,
        });

    public static string Concat(this IEnumerable<string> strings, string separator = "") =>
        string.Join(separator, strings);

    public static string ToTitleCase(this string str) =>
        CultureInfo.InvariantCulture.TextInfo.ToTitleCase(str);

    public static IVoidArgumentValidationConfiguration CalledWith<T>(this ILogger<T> logger,
        LogLevel level,
        Exception? exception = null) =>
        A.CallTo(logger)
            .Where(call => call.Method.Name == "Log" &&
                           call.GetArgument<LogLevel>("logLevel") == level &&
                           call.GetArgument<Exception?>("exception") == exception
            );
}

public static class FakerExtensions
{
    public static string TopicNameString(this Faker faker) =>
        $"{faker.Person.FirstName}_{faker.Random.Guid():N}".ToLowerInvariant();

    internal static TopicId TopicName(this Faker faker, NaviConfig config) =>
        new(faker.TopicNameString(), config);

    public static IEnumerable<int> Range(this Faker faker, int min, int max) =>
        Enumerable.Range(0, faker.Random.Int(min, max));

    public static IEnumerable<int> Range(this Faker faker, int max) =>
        faker.Range(0, max);

    public static IEnumerable<T> Items<T>(this Randomizer faker, IEnumerable<T> items,
        int? count = null) =>
        faker.ListItems(items.ToList(), count);
}

public static class FluentAssertionsComparer
{
    public static T IsEquivalentTo<T>(this IArgumentConstraintManager<T> compare, T value)
    {
        var message = string.Empty;
        return compare.Matches(x => CompareByValue(x, value, out message), x => x.Write(message));
    }

    public static T IsEquivalentInOrder<T>(this IArgumentConstraintManager<T> compare, T value)
    {
        var message = string.Empty;
        return compare.Matches(x => CompareByValue(x, value, out message, true),
            x => x.Write(message));
    }

    static bool CompareByValue<T>(T sut, T expected, out string message,
        bool strictOrdering = false)
    {
        try
        {
            if (strictOrdering)
                sut.Should().BeEquivalentTo(expected, opt => opt.WithStrictOrdering());
            else
                sut.Should().BeEquivalentTo(expected);

            message = string.Empty;
            return true;
        }
        catch (Exception e)
        {
            message = e.Message;
            return false;
        }
    }
}
