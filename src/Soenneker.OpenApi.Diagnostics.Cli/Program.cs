using Microsoft.Extensions.DependencyInjection;
using Soenneker.OpenApi.Diagnostics.Abstract;
using Soenneker.OpenApi.Diagnostics.Models;
using Soenneker.OpenApi.Diagnostics.Registrars;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Soenneker.OpenApi.Diagnostics;

int failOnIndex = Array.IndexOf(args, "--fail-on");
string[] failOn = failOnIndex < 0 ? [] : args[(failOnIndex + 1)..];
if (failOnIndex >= 0) args = args[..failOnIndex];
if (args.Length < 2 || args[0] is not ("spec" or "audit" or "runtime" or "warnings") ||
    failOnIndex >= 0 && (args[0] != "warnings" || failOn.Length == 0))
{
    Console.Error.WriteLine("Usage: spec <openapi.json> [report.json] | audit <spec.json> <generated-directory> [report.json] | runtime <spec.json> <client.dll> [report.json] | warnings <kiota.log> [report.json] [--fail-on category ...]");
    return 2;
}
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
try
{
    object report;
    bool passed;
    int outputIndex;
    if (args[0] == "spec")
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOpenApiDiagnostics();
        await using ServiceProvider provider = services.BuildServiceProvider();
        var result = await provider.GetRequiredService<IOpenApiDiagnostics>().AnalyzeFile(args[1]);
        report = result; passed = !result.Any(i => i.Severity == DiagnosticSeverity.Error); outputIndex = 2;
    }
    else if (args[0] == "warnings")
    {
        var result = new KiotaLogDiagnostics().Analyze(await File.ReadAllTextAsync(args[1], cancellation.Token));
        report = result;
        passed = !result.HasErrors && !failOn.Any(category => category == "all" ? result.Total > 0 : result.Categories.ContainsKey(category));
        outputIndex = 2;
    }
    else
    {
        if (args.Length < 3) throw new ArgumentException("The generated source directory or client assembly is required.");
        outputIndex = 3;
        if (args[0] == "audit")
        {
            var result = await new OpenApiGeneratedCodeDiagnostics().Analyze(args[1], args[2], cancellation.Token);
            report = result; passed = result.Passed;
        }
        else
        {
            var document = JsonNode.Parse(await File.ReadAllTextAsync(args[1], cancellation.Token))!;
            var result = await new OpenApiRuntimeDiagnostics().Analyze(document, Assembly.LoadFrom(Path.GetFullPath(args[2])), cancellationToken: cancellation.Token);
            report = result; passed = result.Passed;
        }
    }
    cancellation.Token.ThrowIfCancellationRequested();
    if (args.Length > outputIndex + 1) throw new ArgumentException("Too many positional arguments.");
    string json = JsonSerializer.Serialize(report, DiagnosticsJsonContext.Default.GetTypeInfo(report.GetType())!);
    if (args.Length > outputIndex) await File.WriteAllTextAsync(args[outputIndex], json, cancellation.Token);
    else Console.WriteLine(json);
    return passed ? 0 : 1;
}
catch (OperationCanceledException) { return 130; }
catch (Exception error) { Console.Error.WriteLine(error.Message); return 2; }
