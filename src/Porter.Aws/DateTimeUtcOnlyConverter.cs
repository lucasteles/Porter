using System.Text.Json;
using System.Text.Json.Serialization;

namespace Porter;

class DateTimeUtcOnlyConverter : JsonConverter<DateTime>
{
    readonly TimeSpan offset;

    public DateTimeUtcOnlyConverter(TimeZoneInfo? timeZone = null) =>
        offset = timeZone?.BaseUtcOffset ?? TimeZoneInfo.Utc.BaseUtcOffset;

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options) =>
        reader.GetDateTime() switch
        {
            { Kind: DateTimeKind.Utc } utcDate => utcDate,
            { Kind: DateTimeKind.Local } localDate => localDate.ToUniversalTime(),
            { Kind: DateTimeKind.Unspecified } date => new DateTimeOffset(date.Ticks, offset)
                .UtcDateTime,
            _ => throw new IndexOutOfRangeException(nameof(DateTime.Kind)),
        };

    public override void Write(Utf8JsonWriter writer, DateTime value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToUniversalTime());
}
