namespace Soenneker.OpenApi.Diagnostics.Models;

/// <summary>
/// Represents a diagnostic issue found in an OpenAPI document
/// </summary>
public class OpenApiDiagnosticIssue
{
    /// <summary>
    /// The severity level of the issue
    /// </summary>
    public DiagnosticSeverity Severity { get; set; }

    /// <summary>
    /// The category of the issue
    /// </summary>
    public DiagnosticCategory Category { get; set; }

    /// <summary>
    /// A unique code identifying the type of issue
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// A detailed message describing the issue
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The location in the OpenAPI document where the issue was found
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Additional details about the issue
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// The path to the component where the issue was found
    /// </summary>
    public string ComponentPath { get; set; } = string.Empty;

    /// <summary>
    /// The name of the component where the issue was found
    /// </summary>
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>
    /// The type of component where the issue was found
    /// </summary>
    public string ComponentType { get; set; } = string.Empty;
}

