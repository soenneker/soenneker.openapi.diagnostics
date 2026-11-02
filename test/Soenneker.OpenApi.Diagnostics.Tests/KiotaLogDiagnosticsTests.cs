using AwesomeAssertions;
using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics.Tests;

public sealed class KiotaLogDiagnosticsTests
{
    [Test]
    public void Preserves_unknown_multiline_diagnostics_and_error_severity()
    {
        KiotaDiagnosticReport report = new KiotaLogDiagnostics().Analyze("""
            warn: Kiota.Builder.KiotaBuilder[1]
                  Choice is a polymorphic type but does not define a discriminator.
            warn: Kiota.Builder.KiotaBuilder[2]
                  New diagnostic we do not yet recognize.
                  Additional context.
            fail: Kiota.Builder.KiotaBuilder[8]
                  Could not create error type for 429 in Get
            """);
        report.Total.Should().Be(3);
        report.Categories["missing-discriminator"].Should().Be(1);
        report.Categories["other"].Should().Be(1);
        report.Categories["error-type"].Should().Be(1);
        report.Diagnostics[1].Message.Should().Be("New diagnostic we do not yet recognize. Additional context.");
        report.Diagnostics[2].Severity.Should().Be("fail");
        report.HasErrors.Should().BeTrue();
    }

    [Test]
    public void Timestamp_wrappers_inline_messages_and_brackets_preserve_event_codes()
    {
        KiotaDiagnosticReport report = new KiotaLogDiagnostics().Analyze("""
            [00:06:57 INF] warn: Kiota.Builder.KiotaBuilder[42]
            [00:06:57 INF] OpenAPI warning: schema is not inherited from Parent [mapping].
            [00:06:57 INF] info: Kiota.Builder.KiotaBuilder[0] Done
            crit: Kiota.Builder.KiotaBuilder[9] failure [with context]
            """);
        report.Total.Should().Be(2);
        report.Diagnostics[0].Code.Should().Be("42");
        report.Diagnostics[0].Category.Should().Be("discriminator-inheritance");
        report.Diagnostics[1].Code.Should().Be("9");
        report.Diagnostics[1].Message.Should().Be("failure [with context]");
    }

    [Test]
    public void Clean_logs_are_empty_and_empty_warning_headers_are_not_dropped()
    {
        var analyzer = new KiotaLogDiagnostics();
        analyzer.Analyze("Generation completed successfully\n").Total.Should().Be(0);
        analyzer.Analyze("warn: Kiota.Builder.KiotaBuilder[5]").Total.Should().Be(1);
        analyzer.Analyze("warn: Kiota.Builder.KiotaBuilder[6]\n  The format currency is not supported by Kiota.")
            .Categories["unsupported-format"].Should().Be(1);
    }
}
