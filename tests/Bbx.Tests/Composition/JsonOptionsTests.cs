using System.Text.Json;
using AwesomeAssertions;
using Bbx.Composition;

namespace Bbx.Tests.Composition;

public class JsonOptionsTests
{
    private static readonly object Sample = new
    {
        Workspace = "ws",
        Repository = "myrepo",
        Items = new[] { "one", "two" },
        Nested = new { CreatedOn = "2026-01-01", IsPrivate = true },
    };

    [Fact]
    public void Indented_writes_pretty_printed_json_with_snake_case_keys()
    {
        var output = JsonSerializer.Serialize(Sample, JsonOptions.Indented);

        output.Should().Contain("\n");
        output.Should().Contain("\"workspace\": \"ws\"");
        output.Should().Contain("\"created_on\":");
    }

    [Fact]
    public void Compact_writes_single_line_with_no_whitespace_between_tokens()
    {
        var output = JsonSerializer.Serialize(Sample, JsonOptions.Compact);

        output.Should().NotContain("\n");
        output.Should().Contain("\"workspace\":\"ws\"");
        output.Should().Contain("\"created_on\":\"2026-01-01\"");
    }

    [Fact]
    public void Indented_and_Compact_parse_to_equivalent_JSON()
    {
        var indented = JsonSerializer.Serialize(Sample, JsonOptions.Indented);
        var compact = JsonSerializer.Serialize(Sample, JsonOptions.Compact);

        // Round-trip both forms to a normalised JsonElement representation and
        // compare; whitespace is irrelevant once they're parsed.
        var normalisedIndented = NormaliseJson(indented);
        var normalisedCompact = NormaliseJson(compact);
        normalisedIndented.Should().Be(normalisedCompact);
    }

    [Fact]
    public void Current_returns_Compact_when_UseCompact_is_true_else_Indented()
    {
        var previous = JsonOptions.UseCompact;
        try
        {
            JsonOptions.UseCompact = false;
            JsonOptions.Current.Should().BeSameAs(JsonOptions.Indented);

            JsonOptions.UseCompact = true;
            JsonOptions.Current.Should().BeSameAs(JsonOptions.Compact);
        }
        finally
        {
            JsonOptions.UseCompact = previous;
        }
    }

    private static string NormaliseJson(string s)
    {
        using var doc = JsonDocument.Parse(s);
        return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
        {
            WriteIndented = false,
        });
    }
}
