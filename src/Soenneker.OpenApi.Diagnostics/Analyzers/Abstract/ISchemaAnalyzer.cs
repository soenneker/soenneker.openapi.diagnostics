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
    Task AnalyzeSchemas(OpenApiDocument document, List<OpenApiDiagnosticIssue> issues);
} 