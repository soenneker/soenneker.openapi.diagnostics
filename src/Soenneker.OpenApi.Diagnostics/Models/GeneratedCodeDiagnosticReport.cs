namespace Soenneker.OpenApi.Diagnostics.Models;

/// <summary>Findings from auditing generated C# against a normalized specification.</summary>
public sealed class GeneratedCodeDiagnosticReport
{
    /// <summary>Number of generated source files inspected.</summary>
    public int Files { get; init; }

    /// <summary>Number of matched methods expected to return typed JSON.</summary>
    public int ResponseContracts { get; init; }

    /// <summary>Number of object or UntypedNode properties found.</summary>
    public int UntypedProperties { get; init; }

    /// <summary>Generated asynchronous methods returning streams.</summary>
    public IReadOnlyList<string> StreamMethods { get; init; } = [];

    /// <summary>Request methods that could not be mapped unambiguously to an operation.</summary>
    public IReadOnlyList<string> UnmatchedRoutes { get; init; } = [];

    /// <summary>Syntax and wire-contract failures.</summary>
    public IReadOnlyList<string> Failures { get; init; } = [];

    /// <summary>Whether the audit found no failures.</summary>
    public bool Passed => Failures.Count == 0;
}
