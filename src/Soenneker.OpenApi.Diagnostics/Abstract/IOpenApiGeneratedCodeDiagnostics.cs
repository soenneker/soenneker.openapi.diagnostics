using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics.Abstract;

/// <summary>Audits generated C# syntax and its correspondence to OpenAPI wire contracts without executing the client.</summary>
public interface IOpenApiGeneratedCodeDiagnostics
{
    /// <summary>Checks syntax, scalar model types, nullable enums, and typed JSON success responses.</summary>
    /// <remarks>Returns findings without writing reports or modifying generated files. This is not a substitute for compilation.</remarks>
    /// <param name="specification">Path to the normalized OpenAPI JSON document.</param>
    /// <param name="directory">Directory containing generated C# files.</param>
    /// <param name="cancellationToken">Cancels file reads and traversal.</param>
    /// <returns>Counts, findings, and request methods that could not be matched to the specification.</returns>
    Task<GeneratedCodeDiagnosticReport> Analyze(string specification, string directory, CancellationToken cancellationToken = default);
}
