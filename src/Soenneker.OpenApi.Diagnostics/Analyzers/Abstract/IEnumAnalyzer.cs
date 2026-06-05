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
    Task AnalyzeEnums(OpenApiDocument document, List<OpenApiDiagnosticIssue> issues);
} 