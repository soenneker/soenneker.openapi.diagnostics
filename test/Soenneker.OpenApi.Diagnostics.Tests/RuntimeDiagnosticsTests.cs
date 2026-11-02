using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Soenneker.OpenApi.Diagnostics.Abstract;
using Soenneker.OpenApi.Diagnostics.Models;
using Soenneker.OpenApi.Diagnostics.Registrars;

namespace Soenneker.OpenApi.Diagnostics.Tests;

public sealed class RuntimeDiagnosticsTests
{
    [Test]
    public async Task Reports_runtime_loss_without_throwing_or_discarding_successful_models()
    {
        JsonNode document = JsonNode.Parse("""
            {"components":{"schemas":{
              "RoundTripModel":{"type":"object","properties":{"name":{"type":"string"}},
                "x-verification-payloads":[{"name":"hello","future":17}]},
              "LossyModel":{"type":"object","properties":{"name":{"type":"string"}}}
            }}}
            """)!;
        var report = await new OpenApiRuntimeDiagnostics().Analyze(document, typeof(RuntimeDiagnosticsTests).Assembly,
            new GeneratedClientRuntimeOptions { ModelsNamespaceSuffix = ".Fixtures" });
        report.Models.Should().Be(2);
        report.Payloads.Should().Be(2);
        report.Passed.Should().BeFalse();
        report.Failures.Should().ContainSingle(f => f.StartsWith("LossyModel: Payload changed:"));
    }

    [Test]
    public async Task Honors_cancellation_and_registers_all_diagnostic_services()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOpenApiDiagnostics();
        await using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IOpenApiDiagnostics>().Should().BeOfType<OpenApiDiagnostics>();
        provider.GetRequiredService<IKiotaLogDiagnostics>().Should().BeOfType<KiotaLogDiagnostics>();
        provider.GetRequiredService<IOpenApiGeneratedCodeDiagnostics>().Should().BeOfType<OpenApiGeneratedCodeDiagnostics>();
        IOpenApiRuntimeDiagnostics diagnostics = provider.GetRequiredService<IOpenApiRuntimeDiagnostics>();
        Func<Task> run = () => diagnostics.Analyze(new JsonObject(), typeof(RuntimeDiagnosticsTests).Assembly,
            cancellationToken: new CancellationToken(true));
        await run.Should().ThrowAsync<OperationCanceledException>();
    }
}
