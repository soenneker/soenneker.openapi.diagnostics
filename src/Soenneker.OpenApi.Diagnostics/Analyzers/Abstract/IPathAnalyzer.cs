using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.OpenApi;
using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics.Analyzers.Abstract;

/// <summary>
/// Analyzes path definitions in OpenAPI documents
/// </summary>
public interface IPathAnalyzer
{
    /// <summary>
    /// Analyzes all paths in the document
    /// </summary>
    /// <param name="document">Document to read, persist, or update.</param>
    /// <param name="issues">issues to process.</param>
    /// <returns>A task that completes when the analyze paths operation is complete.</returns>
    Task AnalyzePaths(OpenApiDocument document, List<OpenApiDiagnosticIssue> issues);
}
