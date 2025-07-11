using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.OpenApi;
using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics.Analyzers.Abstract;

public interface IPathAnalyzer
{
    Task AnalyzePaths(OpenApiDocument document, List<OpenApiDiagnosticIssue> issues);
} 