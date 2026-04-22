using System.Collections.Generic;
using Soenneker.Tests.Attributes.Local;
using Soenneker.Tests.HostedUnit;
using System.Threading.Tasks;
using Soenneker.Utils.Json;
using System.IO;
using System.Linq;
using Soenneker.OpenApi.Diagnostics.Abstract;
using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class OpenApiDiagnosticsTests : HostedUnitTest
{
    private readonly IOpenApiDiagnostics _util;

    public OpenApiDiagnosticsTests(Host host) : base(host)
    {
        _util = Resolve<IOpenApiDiagnostics>(true);
    }

    [Test]
    public void Default()
    {

    }

    [LocalOnly]
    public async ValueTask AnalyzeFile()
    {
        List<OpenApiDiagnosticIssue> issues = await _util.AnalyzeFile(@"c:\cloudflare\spec3fixed.json");

        List<OpenApiDiagnosticIssue> errors = issues.Where(x => x.Severity == DiagnosticSeverity.Error && x.Category != DiagnosticCategory.Naming).ToList();

        string? output = JsonUtil.Serialize(errors, Enums.JsonOptions.JsonOptionType.Pretty, Enums.JsonLibrary.JsonLibraryType.SystemTextJson);

        File.Delete("c:\\cloudflare\\problems.txt");

        await File.WriteAllTextAsync(@"c:\cloudflare\problems.txt", output, CancellationToken);

       // Logger.LogInformation(output);
    }
}
