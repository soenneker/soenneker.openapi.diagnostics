using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics.Analyzers.Abstract;

public interface IEnumAnalyzer
{
    Task AnalyzeEnums(OpenApiDocument document, List<OpenApiDiagnosticIssue> issues);
} 