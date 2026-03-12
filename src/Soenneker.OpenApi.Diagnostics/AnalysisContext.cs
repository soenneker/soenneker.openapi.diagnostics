using Microsoft.OpenApi;
using Soenneker.OpenApi.Diagnostics.Models;
using System;
using System.Collections.Generic;

namespace Soenneker.OpenApi.Diagnostics;

/// <summary>
/// Holds the state for a single analysis run to ensure the main class is stateless.
/// </summary>
internal class AnalysisContext
{
    public OpenApiDocument Document { get; }
    public List<OpenApiDiagnosticIssue> Issues { get; } = new();
    public HashSet<string> OperationIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, HashSet<string>> SchemaDependencies { get; } = new();
    public HashSet<string> ReportedCycles { get; } = new();

    public AnalysisContext(OpenApiDocument document)
    {
        Document = document;
    }

    public void AddIssue(DiagnosticSeverity severity, DiagnosticCategory category, string code, string message, string location,
        string componentName = null, string componentPath = null, string componentType = null)
    {
        Issues.Add(new OpenApiDiagnosticIssue
        {
            Severity = severity,
            Category = category,
            Code = code,
            Message = message,
            Location = location,
            ComponentName = componentName,
            ComponentPath = componentPath,
            ComponentType = componentType
        });
    }
}