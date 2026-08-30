using System.Collections.Generic;
using AwesomeAssertions;
using Soenneker.Tests.HostedUnit;
using System.Linq;
using System.Threading.Tasks;
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

    [Test]
    public async ValueTask Analyze_returns_client_generation_issues_for_json()
    {
        const string json = """
                            {
                              "openapi": "3.0.3",
                              "info": { "title": "Example", "version": "1.0" },
                              "paths": {
                                "/items": {
                                  "get": { "responses": { "200": { "description": "OK" } } }
                                }
                              }
                            }
                            """;

        List<OpenApiDiagnosticIssue> issues = await _util.Analyze(json);

        issues.Any(issue => issue.Code == "MISSING_OPERATION_ID").Should().BeTrue();
    }
}

