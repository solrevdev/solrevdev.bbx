using System.Text.Json;
using AwesomeAssertions;
using Bbx.Features.Pipelines;

namespace Bbx.Tests.Features.Pipelines;

public class PipelineFormatTests
{
    [Fact]
    public void Pipeline_accepts_null_result_and_target_members()
    {
        var pipeline = JsonDocument.Parse("""
            {
              "uuid":"{p1}",
              "build_number":7,
              "state":{"name":"IN_PROGRESS","result":null},
              "target":{"ref_type":"branch","ref_name":"main","commit":null},
              "trigger":null,
              "duration_in_seconds":null
            }
            """).RootElement;

        var result = PipelineFormat.Pipeline(pipeline);

        result.State!.Name.Should().Be("IN_PROGRESS");
        result.State.Result.Should().BeNull();
        result.Target!.Commit.Should().BeNull();
        result.Trigger.Should().BeNull();
        result.DurationInSeconds.Should().BeNull();
    }

    [Fact]
    public void PipelineDetailed_keeps_the_existing_flat_json_shape()
    {
        var pipeline = JsonDocument.Parse("""
            {"uuid":"{p1}","build_number":7,"state":null,"target":null,
             "creator":null,"repository":null,"links":null}
            """).RootElement;

        var json = JsonSerializer.Serialize(PipelineFormat.PipelineDetailed(pipeline),
            Bbx.Composition.JsonOptions.Current);

        json.Should().Contain("\"uuid\": \"{p1}\"")
            .And.Contain("\"build_number\": 7")
            .And.NotContain("\"pipeline\"");
    }

    [Fact]
    public void Step_accepts_null_state()
    {
        var step = JsonDocument.Parse("""{"uuid":"{s1}","state":null}""").RootElement;

        var result = PipelineFormat.Step(step);

        result.Uuid.Should().Be("{s1}");
        result.State.Should().BeNull();
    }
}
