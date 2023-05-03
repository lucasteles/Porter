namespace Porter.Models;

public sealed record TopicNameOverride(
    string? Suffix = null, string? Prefix = null)
{
    public bool HasValues() => this is (not null, _) or (_, not null);
}
