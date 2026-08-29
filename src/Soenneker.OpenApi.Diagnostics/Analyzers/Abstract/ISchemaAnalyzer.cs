using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.OpenApi;
using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics.Analyzers.Abstract;

/// <summary>
/// Analyzes schema definitions in OpenAPI documents
/// </summary>
public interface ISchemaAnalyzer
{
    /// <summary>
    /// Analyzes all schemas in the document
    /// </summary>
    /// <param name="document">Document to read, persist, or update.</param>
    /// <param name="issues">issues to process.</param>
    /// <returns>A task that completes when the analyze schemas operation is complete.</returns>
    Task AnalyzeSchemas(OpenApiDocument document, List<OpenApiDiagnosticIssue> issues);
}
