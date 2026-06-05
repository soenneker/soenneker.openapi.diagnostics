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
    Task AnalyzePaths(OpenApiDocument document, List<OpenApiDiagnosticIssue> issues);
} 