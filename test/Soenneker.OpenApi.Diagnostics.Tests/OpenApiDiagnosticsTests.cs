using System.Collections.Generic;
using Soenneker.Facts.Local;
using Soenneker.Tests.FixturedUnit;
using System.Threading.Tasks;
using Soenneker.Utils.Json;
using Xunit;
using System.IO;
using System.Linq;
using Soenneker.OpenApi.Diagnostics.Abstract;
using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics.Tests;

[Collection("Collection")]
public sealed class OpenApiDiagnosticsTests : FixturedUnitTest
{
    private readonly IOpenApiDiagnostics _util;

    public OpenApiDiagnosticsTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IOpenApiDiagnostics>(true);
    }

    [Fact]
    public void Default()
    {

    }

    [LocalFact]
    public async ValueTask AnalyzeFile()
    {
        List<OpenApiDiagnosticIssue> issues = await _util.AnalyzeFile(@"c:\cloudflare\fixed.json");

        List<OpenApiDiagnosticIssue> errors = issues.Where(x => x.Severity == DiagnosticSeverity.Error && x.Category != DiagnosticCategory.Naming).ToList();

        string? output = JsonUtil.Serialize(errors, Enums.JsonOptions.JsonOptionType.Pretty, Enums.JsonLibrary.JsonLibraryType.SystemTextJson);

        File.Delete("c:\\cloudflare\\problems.txt");

        await File.WriteAllTextAsync(@"c:\cloudflare\problems.txt", output, CancellationToken);

       // Logger.LogInformation(output);
    }
}
