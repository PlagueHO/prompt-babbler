using FluentAssertions;
using PromptBabbler.ServiceDefaults.Logging;

namespace PromptBabbler.ServiceDefaults.UnitTests.Logging;

[TestClass]
[TestCategory("Unit")]
public sealed class LogSanitizerTests
{
    [TestMethod]
    public void Sanitize_WithNullValue_ReturnsNull()
    {
        var result = LogSanitizer.Sanitize(null);

        result.Should().BeNull();
    }

    [TestMethod]
    public void Sanitize_WithSafeText_ReturnsOriginalString()
    {
        const string input = "safe text 123";

        var result = LogSanitizer.Sanitize(input);

        result.Should().BeSameAs(input);
    }

    [TestMethod]
    public void Sanitize_WithControlCharacters_EscapesControlCharacters()
    {
        const string input = "line1\r\nline2\t\u0001";

        var result = LogSanitizer.Sanitize(input);

        result.Should().Be("line1\\r\\nline2\\t\\u0001");
    }

    [TestMethod]
    public void SanitizeAttributes_WithNullAttributes_ReturnsNull()
    {
        var result = LogSanitizer.SanitizeAttributes(null);

        result.Should().BeNull();
    }

    [TestMethod]
    public void SanitizeAttributes_WithSafeStringAttributes_ReturnsOriginalReference()
    {
        IReadOnlyList<KeyValuePair<string, object?>> attributes =
        [
            new KeyValuePair<string, object?>("textLength", "12"),
            new KeyValuePair<string, object?>("attempt", 1),
        ];

        var result = LogSanitizer.SanitizeAttributes(attributes);

        result.Should().BeSameAs(attributes);
    }

    [TestMethod]
    public void SanitizeAttributes_WithUnsafeStringAttribute_SanitizesOnlyStringValues()
    {
        IReadOnlyList<KeyValuePair<string, object?>> attributes =
        [
            new KeyValuePair<string, object?>("message", "hello\nworld"),
            new KeyValuePair<string, object?>("count", 2),
        ];

        var result = LogSanitizer.SanitizeAttributes(attributes);

        result.Should().NotBeSameAs(attributes);
        result.Should().HaveCount(2);
        result![0].Key.Should().Be("message");
        result[0].Value.Should().Be("hello\\nworld");
        result[1].Key.Should().Be("count");
        result[1].Value.Should().Be(2);
    }
}
