namespace Soenneker.OpenApi.Diagnostics.Models;

/// <summary>All parsed generator diagnostics and their category counts.</summary>
public sealed class KiotaDiagnosticReport
{
    /// <summary>Entries in their original log order.</summary>
    public IReadOnlyList<KiotaDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>Diagnostic counts by stable category name.</summary>
    public IReadOnlyDictionary<string, int> Categories { get; init; } = new Dictionary<string, int>();

    /// <summary>Total number of warning and error entries.</summary>
    public int Total => Diagnostics.Count;

    /// <summary>Whether fail, error, or crit entries were found.</summary>
    public bool HasErrors => Diagnostics.Any(d => d.Severity is "fail" or "error" or "crit");
}
