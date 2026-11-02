using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics.Abstract;

/// <summary>Classifies Kiota generator diagnostics while preserving unrecognized messages.</summary>
public interface IKiotaLogDiagnostics
{
    /// <summary>Parses warning and error entries, including indented continuation lines and timestamp-prefixed console output.</summary>
    /// <param name="log">Complete generator console output.</param>
    /// <returns>Every diagnostic with its original severity, event code, message, and classification.</returns>
    KiotaDiagnosticReport Analyze(string log);
}
