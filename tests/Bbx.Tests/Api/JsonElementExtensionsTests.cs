using System.Text.Json;
using AwesomeAssertions;
using Bbx.Api;

namespace Bbx.Tests.Api;

public class JsonElementExtensionsTests
{
    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void TryGetObject_returns_nested_object()
    {
        var ok = Parse("""{"target":{"hash":"abc"}}""").TryGetObject("target", out var target);

        ok.Should().BeTrue();
        target.GetProperty("hash").GetString().Should().Be("abc");
    }

    // Regression: a lightweight tag has "tagger": null. TryGetProperty returns
    // true for that, and reading a property off it threw InvalidOperationException.
    [Fact]
    public void TryGetObject_returns_false_for_explicit_json_null()
    {
        var element = Parse("""{"tagger":null}""");

        element.TryGetProperty("tagger", out _).Should().BeTrue("TryGetProperty accepts null");
        element.TryGetObject("tagger", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("""{"target":"a string"}""")]
    [InlineData("""{"target":[1,2]}""")]
    [InlineData("""{"target":7}""")]
    [InlineData("""{"other":{}}""")]
    public void TryGetObject_returns_false_when_not_an_object(string json)
    {
        Parse(json).TryGetObject("target", out _).Should().BeFalse();
    }

    [Fact]
    public void TryGetObject_returns_false_when_receiver_is_not_an_object()
    {
        Parse("""["a"]""").TryGetObject("target", out _).Should().BeFalse();
        Parse("null").TryGetObject("target", out _).Should().BeFalse();
    }

    [Fact]
    public void TryGetObject_chains_safely_through_a_null_link()
    {
        var element = Parse("""{"tagger":null}""");

        var name = element.TryGetObject("tagger", out var tagger)
                   && tagger.TryGetObject("user", out var user)
                   && user.TryGetProperty("display_name", out var dn)
            ? dn.GetString()
            : null;

        name.Should().BeNull();
    }

    [Fact]
    public void GetStringOrNull_reads_strings_and_tolerates_null_and_wrong_types()
    {
        Parse("""{"name":"main"}""").GetStringOrNull("name").Should().Be("main");
        Parse("""{"name":null}""").GetStringOrNull("name").Should().BeNull();
        Parse("""{"name":42}""").GetStringOrNull("name").Should().BeNull();
        Parse("{}").GetStringOrNull("name").Should().BeNull();
    }
}
