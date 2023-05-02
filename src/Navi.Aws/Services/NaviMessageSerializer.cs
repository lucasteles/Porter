using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Navi.Extensions;

namespace Navi.Services;

public interface INaviMessageSerializer
{
    string Serialize<TValue>(TValue something);
    TValue Deserialize<TValue>(ReadOnlySpan<char> json);
    object? Deserialize(Type type, ReadOnlySpan<char> json);
}

public interface INaviJsonSerializerOptions
{
    public JsonSerializerOptions Get();
}

public interface INaviJsonSerializerConverters
{
    public IReadOnlyCollection<JsonConverter> Get();
}

class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public static SnakeCaseNamingPolicy Instance { get; } = new();
    public override string ConvertName(string name) => name.ToSnakeCase();
}

class NaviDefaultJsonSerializerConverters : INaviJsonSerializerConverters
{
    static readonly HashSet<JsonConverter> converters = new()
    {
        new JsonStringEnumConverter(),
        new DateTimeUtcOnlyConverter(),
    };

    internal static void Clear() => converters.Clear();

    public IReadOnlyCollection<JsonConverter> Get() => converters;
}

record NaviJsonSerializerConverters
    (IReadOnlyCollection<JsonConverter> Converters) : INaviJsonSerializerConverters
{
    public IReadOnlyCollection<JsonConverter> Get() => Converters;
}

class NaviJsonSerializerOptions : INaviJsonSerializerOptions
{
    readonly JsonSerializerOptions jsonOptions;

    public NaviJsonSerializerOptions(IEnumerable<INaviJsonSerializerConverters> converters)
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

class NaviJsonSerializer : INaviMessageSerializer
{
    readonly INaviJsonSerializerOptions options;
    public NaviJsonSerializer(INaviJsonSerializerOptions options) => this.options = options;

    public string Serialize<TValue>(TValue something) =>
        JsonSerializer.Serialize(something, options.Get());

    public TValue Deserialize<TValue>(ReadOnlySpan<char> json) =>
        JsonSerializer.Deserialize<TValue>(json, options.Get()) ??
        throw new SerializationException("Unable to deserialize message");

    public object Deserialize(Type type, ReadOnlySpan<char> json) =>
        JsonSerializer.Deserialize(json, type, options.Get()) ??
        throw new SerializationException("Unable to deserialize message");
}
