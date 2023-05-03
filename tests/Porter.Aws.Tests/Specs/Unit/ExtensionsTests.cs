using Porter.Extensions;

namespace Porter.Aws.Tests.Specs.Unit;

public class ExtensionsTests
{
    [TestCase("a_snake_name", "ASnakeName")]
    [TestCase("c99_client_process_with_pendencies", "C99ClientProcessWithPendencies")]
    [TestCase("aBc_DeF", "AbcDef")]
    public void SnakeToPascalCaseTests(string name, string expected) => name.ToPascalCase().Should().Be(expected);
}
