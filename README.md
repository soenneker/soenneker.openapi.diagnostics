[![](https://img.shields.io/nuget/v/soenneker.openapi.diagnostics.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.openapi.diagnostics/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.openapi.diagnostics/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.openapi.diagnostics/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.openapi.diagnostics.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.openapi.diagnostics/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.OpenApi.Diagnostics

A comprehensive OpenAPI document diagnostic utility that helps identify issues that could affect client generation and API documentation.

## Features

### Top-Level Showstopper Issues
- Empty or Invalid OperationId detection
- Invalid $ref identifier validation
- Empty enum array detection
- Schema name conflict detection
- Polymorphic type validation
- Path parameter validation
- Recursive model validation
- Default value validation
- Empty schema detection

### Mid-Level Problems
- allOf usage validation
- Name collision detection
- Schema combination validation
- Format/type combination validation
- OpenAPI version validation

### Subtle Problems
- Discriminator mapping validation
- Description/summary validation
- Parameter/response reference validation
- Media type validation
- Request body validation

### Kiota-Specific Issues
- File size validation
- Empty key detection
- Nullable property validation
- Enum type validation
- Discriminator property validation

## Installation

```bash
dotnet add package Soenneker.OpenApi.Diagnostics
```

## Usage

### Basic Usage

```csharp
using Microsoft.Extensions.DependencyInjection;
using Soenneker.OpenApi.Diagnostics;
using Soenneker.OpenApi.Diagnostics.Abstract;

// Register the service
services.AddOpenApiDiagnostics();

// Use the service
public class MyService
{
    private readonly IOpenApiDiagnostics _diagnostics;

    public MyService(IOpenApiDiagnostics diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public async Task AnalyzeOpenApiDocument(string jsonContent)
    {
        var issues = await _diagnostics.AnalyzeJson(jsonContent);
        
        foreach (var issue in issues)
        {
            Console.WriteLine($"Severity: {issue.Severity}");
            Console.WriteLine($"Category: {issue.Category}");
            Console.WriteLine($"Code: {issue.Code}");
            Console.WriteLine($"Message: {issue.Message}");
            Console.WriteLine($"Location: {issue.Location}");
            if (issue.Details != null)
                Console.WriteLine($"Details: {issue.Details}");
            Console.WriteLine();
        }
    }
}
```

### Analyzing a File

```csharp
var issues = await _diagnostics.AnalyzeFile("path/to/openapi.json");
```

### Analyzing a Document

```csharp
var document = new OpenApiDocument();
// ... populate document ...
var issues = await _diagnostics.AnalyzeDocument(document);
```

## Diagnostic Categories

The library categorizes issues into the following categories:

- OperationId
- Reference
- Enum
- SchemaNaming
- PolymorphicType
- PathParameter
- RecursiveModel
- DefaultValue
- EmptySchema
- SchemaCombination
- FormatType
- Version
- Discriminator
- Description
- MediaType
- RequestBody
- FileSize
- EmptyKey
- Nullable
- Other

## Severity Levels

Issues are classified into three severity levels:

- Error: Critical issues that will prevent client generation
- Warning: Issues that may cause problems but won't prevent generation
- Info: Issues that may affect functionality but are not critical

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
