using Porter.Services;

namespace Porter.Aws.Tests.TestUtils;

class FakeSerializer : IPorterMessageSerializer
{
    readonly object? returnValue;

    public FakeSerializer(object? returnValue) => this.returnValue = returnValue;
    public string Serialize<TValue>(TValue something) => default!;

    public TValue Deserialize<TValue>(ReadOnlySpan<char> json) => (TValue)returnValue!;

    public object? Deserialize(Type type, ReadOnlySpan<char> json) => returnValue;
}
