using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Porter.Extensions;

namespace Porter.Services;

public interface IPorterMessageSerializer
{
    string Serialize<TValue>(TValue something);
    TValue Deserialize<TValue>(ReadOnlySpan<char> json);
    object? Deserialize(Type type, ReadOnlySpan<char> json);
}

public interface IPorterJsonSerializerOptions
{
    public JsonSerializerOptions Get();
}

public interface IPorterJsonSerializerConverters
{
    public IReadOnlyCollection<JsonConverter> Get();
}

class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public static SnakeCaseNamingPolicy Instance { get; } = new();
    public override string ConvertName(string name) => name.ToSnakeCase();
}

class PorterDefaultJsonSerializerConverters : IPorterJsonSerializerConverters
{
    static readonly HashSet<JsonConverter> converters = new()
    {
        new JsonStringEnumConverter(),
        new DateTimeUtcOnlyConverter(),
    };

    internal static void Clear() => converters.Clear();

    public IReadOnlyCollection<JsonConverter> Get() => converters;
}

record PorterJsonSerializerConverters
    (IReadOnlyCollection<JsonConverter> Converters) : IPorterJsonSerializerConverters
{
    public IReadOnlyCollection<JsonConverter> Get() => Converters;
}

class PorterJsonSerializerOptions : IPorterJsonSerializerOptions
{
    readonly JsonSerializerOptions jsonOptions;

    public PorterJsonSerializerOptions(IEnumerable<IPorterJsonSerializerConverters> converters)
    {
        jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
        };

        foreach (var converter in converters.SelectMany(x => x.Get()).ToList())
            jsonOptions.Converters.Add(converter);
    }

    public JsonSerializerOptions Get() => jsonOptions;
}

class PorterJsonSerializer : IPorterMessageSerializer
{
    readonly IPorterJsonSerializerOptions options;
    public PorterJsonSerializer(IPorterJsonSerializerOptions options) => this.options = options;

    public string Serialize<TValue>(TValue something) =>
        JsonSerializer.Serialize(something, options.Get());

    public TValue Deserialize<TValue>(ReadOnlySpan<char> json) =>
        JsonSerializer.Deserialize<TValue>(json, options.Get()) ??
        throw new SerializationException("Unable to deserialize message");

    public object Deserialize(Type type, ReadOnlySpan<char> json) =>
        JsonSerializer.Deserialize(json, type, options.Get()) ??
        throw new SerializationException("Unable to deserialize message");
}
