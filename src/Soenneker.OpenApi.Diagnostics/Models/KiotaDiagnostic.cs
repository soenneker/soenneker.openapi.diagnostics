namespace Soenneker.OpenApi.Diagnostics.Models;

/// <summary>One generator log entry, preserving its severity, event identifier, and complete message.</summary>
public sealed class KiotaDiagnostic
{
    /// <summary>Original console severity: warn, fail, error, or crit.</summary>
    public string Severity { get; init; } = "";

    /// <summary>Generator event identifier.</summary>
    public string Code { get; init; } = "";

    /// <summary>Stable classification such as missing-discriminator; other preserves unrecognized diagnostics.</summary>
    public string Category { get; init; } = "other";

    /// <summary>Joined message and continuation lines.</summary>
    public string Message { get; init; } = "";
}
