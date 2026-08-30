[![](https://img.shields.io/nuget/v/soenneker.openapi.diagnostics.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.openapi.diagnostics/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.openapi.diagnostics/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.openapi.diagnostics/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.openapi.diagnostics.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.openapi.diagnostics/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.openapi.diagnostics/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.openapi.diagnostics/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.OpenApi.Diagnostics

Analyze OpenAPI documents for structural problems and patterns that commonly break generated clients.

## Installation

```bash
dotnet add package Soenneker.OpenApi.Diagnostics
```

## Registration

```csharp
using Microsoft.Extensions.DependencyInjection;
using Soenneker.OpenApi.Diagnostics.Registrars;

services.AddOpenApiDiagnostics();
```

`AddOpenApiDiagnostics()` registers the analyzer as a singleton. Use `AddOpenApiDiagnosticsAsScoped()` when the analyzer should follow a dependency-injection scope.

## Analyze JSON

Inject `IOpenApiDiagnostics` and pass a complete JSON document:

```csharp
using Soenneker.OpenApi.Diagnostics.Abstract;
using Soenneker.OpenApi.Diagnostics.Models;

List<OpenApiDiagnosticIssue> issues = await diagnostics.Analyze(openApiJson);

foreach (OpenApiDiagnosticIssue issue in issues)
{
    Console.WriteLine($"{issue.Severity} {issue.Code} at {issue.Location}: {issue.Message}");
}
```

Parse failures are returned as `PARSE_ERROR` issues rather than thrown. Unexpected analysis failures are returned as `UNEXPECTED_ERROR` or `UNEXPECTED_ANALYSIS_ERROR` issues.

## Analyze a file

```csharp
List<OpenApiDiagnosticIssue> issues = await diagnostics.AnalyzeFile("openapi.json");
```

You can also analyze a `FileInfo`, a readable `Stream`, or an already parsed `OpenApiDocument` through the corresponding `Analyze` overload. Stream ownership remains with the caller.

## What is checked

Diagnostics cover:

- required document metadata, paths, operations, and responses
- missing, invalid, or duplicate operation IDs
- path placeholders and path-parameter definitions
- duplicate parameters and missing parameter schemas
- inline complex schemas and binary schemas in JSON media types
- schema names, C# keyword collisions, and normalized-name collisions
- discriminator requirements and `oneOf` usage relevant to Kiota
- empty enum values and untyped object schemas
- circular component dependencies
- OAuth flow definitions and undefined operation tags

Each issue includes a stable `Code`, `Severity`, `Category`, human-readable `Message`, and document `Location`. Component fields are populated when the issue belongs to a specific component.

## Interpreting results

- `Error` identifies an invalid construct or a condition expected to break supported generation scenarios.
- `Warning` identifies a risky or weakly typed construct that may still be accepted.
- `Info` identifies non-blocking contract quality issues.

The analyzer is intentionally opinionated about generated-client compatibility, especially Kiota. A clean result is not a substitute for validating the contract against the running API or testing the generated client.
