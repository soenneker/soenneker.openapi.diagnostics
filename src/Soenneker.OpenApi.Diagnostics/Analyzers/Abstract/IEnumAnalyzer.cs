using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.OpenApi;
using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics.Analyzers.Abstract;

/// <summary>
/// Analyzes enum definitions in OpenAPI documents
/// </summary>
public interface IEnumAnalyzer
{
    /// <summary>
    /// Analyzes all enums in the document
    /// </summary>
    /// <param name="document">Document to read, persist, or update.</param>
    /// <param name="issues">issues to process.</param>
    /// <returns>A task that completes when the analyze enums operation is complete.</returns>
    Task AnalyzeEnums(OpenApiDocument document, List<OpenApiDiagnosticIssue> issues);
}
