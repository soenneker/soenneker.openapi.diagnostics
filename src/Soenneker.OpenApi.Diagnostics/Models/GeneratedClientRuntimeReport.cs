namespace Soenneker.OpenApi.Diagnostics.Models;

/// <summary>Results of exercising generated model serialization and request contracts.</summary>
public sealed class GeneratedClientRuntimeReport
{
    /// <summary>Number of generated models inspected.</summary>
    public int Models { get; init; }

    /// <summary>Number of successful serialization round trips.</summary>
    public int Payloads { get; init; }

    /// <summary>Number of models exercised with automatically constructed scalar samples.</summary>
    public int SampledModels { get; init; }

    /// <summary>Number of successful explicit request contracts.</summary>
    public int Requests { get; init; }

    /// <summary>Model, dispatch, serialization, and request-contract failures.</summary>
    public IReadOnlyList<string> Failures { get; init; } = [];

    /// <summary>Whether all exercised contracts passed.</summary>
    public bool Passed => Failures.Count == 0;
}
