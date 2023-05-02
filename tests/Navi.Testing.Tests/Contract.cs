namespace Navi.Testing.Tests;

public class MyMessage1
{
    public Guid Id { get; set; }
    public string Foo { get; set; } = "";

    public string ToJson() => $@"{{""id"":""{Id}"",""foo"":""{Foo}""}}";
}

public record MyMessage2
{
    public Guid Id { get; set; }
    public string Bar { get; set; } = "";
    public string ToJson() => $@"{{""id"":""{Id}"",""bar"":""{Bar}""}}";
}
