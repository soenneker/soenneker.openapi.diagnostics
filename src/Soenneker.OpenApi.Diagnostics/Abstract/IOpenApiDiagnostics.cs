using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.OpenApi;
using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics.Abstract;

/// <summary>
/// Analyzes OpenAPI documents for structural and client-generation problems.
/// </summary>
public interface IOpenApiDiagnostics
{
    /// <summary>
    /// Analyzes an OpenAPI document from a JSON string.
    /// </summary>
    /// <param name="openApiJson">The complete OpenAPI JSON document.</param>
    /// <returns>The diagnostics found in the document, including parse errors.</returns>
    ValueTask<List<OpenApiDiagnosticIssue>> Analyze(string openApiJson);

    /// <summary>
    /// Analyzes an OpenAPI JSON document from a file path.
    /// </summary>
    /// <param name="file">The file to read.</param>
    /// <returns>The diagnostics found in the document.</returns>
    ValueTask<List<OpenApiDiagnosticIssue>> AnalyzeFile(string file);

    /// <summary>
    /// Reads and analyzes an OpenAPI document from a stream. The stream remains owned by the caller.
    /// </summary>
    /// <param name="openApiStream">The readable OpenAPI document stream.</param>
    /// <returns>The diagnostics found in the document, including parse errors.</returns>
    Task<List<OpenApiDiagnosticIssue>> Analyze(Stream openApiStream);

    /// <summary>
    /// Reads and analyzes an OpenAPI document from a file.
    /// </summary>
    /// <param name="fileInfo">The OpenAPI document file.</param>
    /// <returns>The diagnostics found in the document, or a <c>FILE_NOT_FOUND</c> issue when it does not exist.</returns>
    Task<List<OpenApiDiagnosticIssue>> Analyze(FileInfo fileInfo);

    /// <summary>
    /// Analyzes an already parsed OpenAPI document.
    /// </summary>
    /// <param name="document">The document to analyze.</param>
    /// <returns>The diagnostics found in the document.</returns>
    List<OpenApiDiagnosticIssue> Analyze(OpenApiDocument document);
}
