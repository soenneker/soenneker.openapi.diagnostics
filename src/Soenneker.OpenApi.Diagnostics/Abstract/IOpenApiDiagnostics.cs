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
    /// <param name="openApiJson">Open API JSON for the analyze operation.</param>
    /// <returns>A task whose result is the collection returned by analyze.</returns>
    ValueTask<List<OpenApiDiagnosticIssue>> Analyze(string openApiJson);

    /// <summary>
    /// Analyzes an OpenAPI document from a file
    /// </summary>
    /// <param name="file">File for the analyze file operation.</param>
    /// <returns>A task whose result is the collection returned by analyze File.</returns>
    ValueTask<List<OpenApiDiagnosticIssue>> AnalyzeFile(string file);
}
