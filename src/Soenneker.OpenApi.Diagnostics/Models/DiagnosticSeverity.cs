namespace Soenneker.OpenApi.Diagnostics.Models;

/// <summary>
/// Represents the severity level of a diagnostic issue
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>
    /// Critical issues that will prevent client generation
    /// </summary>
    Error,

    /// <summary>
    /// Issues that may cause problems but won't prevent generation
    /// </summary>
    Warning,

    /// <summary>
    /// Issues that may affect functionality but are not critical
    /// </summary>
    Info
}

