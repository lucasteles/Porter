global using FakeConsumer = Porter.Aws.Tests.Builders.FakeConsumer<string>;
global using TestConsumer =
    Porter.Aws.Tests.Builders.FakeMessageConsumer<
        Porter.Aws.Tests.Builders.TestMessage>;
global using FakeMessageConsumer = Porter.Aws.Tests.Builders.FakeMessageConsumer<string>;
using System.Text.Json;
using AutoBogus;

namespace Porter.Aws.Tests.Builders;

public class TestMessage
{
    public Guid TestId { get; set; }
    public int IntField { get; set; }
    public string? StringField { get; set; }
    public bool BoolField { get; set; }
    public double DoubleField { get; set; }
    public DateTime? DateTimeField { get; set; }

    public string ToSnakeCaseJson() =>
        JsonSerializer.Serialize(new
        {
            test_id = TestId,
            int_field = IntField,
            string_field = StringField,
            bool_field = BoolField,
            double_field = DoubleField,
            date_time_field = DateTimeField?.ToString("o"),
        });

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    });

    public static TestMessage New() =>
        new AutoFaker<TestMessage>()
            .RuleFor(x => x.DateTimeField, f => f.Date.Soon().ToUniversalTime())
            .Generate();

    public IMessageMeta GetMeta() => AutoFaker.Generate<MessageMeta>();
}

public struct TestMessageValue
{
    public Guid TestId { get; set; }
    public int IntField { get; set; }
    public string? StringField { get; set; }
    public bool BoolField { get; set; }
    public double DoubleField { get; set; }
    public DateTime? DateTimeField { get; set; }

    public string ToSnakeCaseJson() =>
        JsonSerializer.Serialize(new
        {
            test_id = TestId,
            int_field = IntField,
            string_field = StringField,
            bool_field = BoolField,
            double_field = DoubleField,
            date_time_field = DateTimeField?.ToString("o"),
        });

    public static TestMessage New() => AutoFaker.Generate<TestMessage>();
}

public class TestMessageSuper : TestMessage
{
    public Guid SuperId { get; set; }
}

public class FakeMessageConsumer<T> : IMessageConsumer<T> where T : notnull
{
    public virtual Task Consume(T message, IMessageMeta meta, CancellationToken ct) =>
        Task.CompletedTask;
}

public class FakeConsumer<T> : IConsumer<T> where T : notnull
{
    public virtual Task Consume(T message, CancellationToken ct) =>
        Task.CompletedTask;
}
