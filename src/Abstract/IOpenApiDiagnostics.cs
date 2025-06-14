using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics.Abstract;

/// <summary>
/// Interface for OpenAPI document diagnostics
/// </summary>
public interface IOpenApiDiagnostics
{
    /// <summary>
    /// Analyzes an OpenAPI document and returns a list of diagnostic issues
    /// </summary>
    /// <param name="document">The OpenAPI document to analyze</param>
    /// <returns>A list of diagnostic issues found in the document</returns>
    Task<List<OpenApiDiagnosticIssue>> AnalyzeDocument(OpenApiDocument document);

    /// <summary>
    /// Analyzes an OpenAPI document from a JSON string
    /// </summary>
    /// <param name="jsonContent">The OpenAPI document as a JSON string</param>
    /// <returns>A list of diagnostic issues found in the document</returns>
    Task<List<OpenApiDiagnosticIssue>> AnalyzeJson(string jsonContent);

    /// <summary>
    /// Analyzes an OpenAPI document from a file path
    /// </summary>
    /// <param name="filePath">Path to the OpenAPI document file</param>
    /// <returns>A list of diagnostic issues found in the document</returns>
    Task<List<OpenApiDiagnosticIssue>> AnalyzeFile(string filePath);
}
