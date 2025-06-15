using System.Collections.Generic;
using System.Threading.Tasks;
using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics.Abstract;

/// <summary>
/// Interface for analyzing OpenAPI documents and identifying potential issues
/// </summary>
public interface IOpenApiDiagnostics
{
    /// <summary>
    /// Analyzes an OpenAPI document from a JSON string
    /// </summary>
    Task<List<OpenApiDiagnosticIssue>> Analyze(string openApiJson);

    /// <summary>
    /// Analyzes an OpenAPI document from a file
    /// </summary>
    Task<List<OpenApiDiagnosticIssue>> AnalyzeFile(string file);
} 