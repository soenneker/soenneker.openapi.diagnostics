using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Nodes;
using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics.Abstract;

/// <summary>Exercises generated Kiota models and request builders against their wire contracts.</summary>
public interface IOpenApiRuntimeDiagnostics
{
    /// <summary>Checks field readers, serialization round trips, tagged-union dispatch, and explicit request contracts.</summary>
    /// <remarks>
    /// Executes code from the supplied assembly; use trusted generated clients. Request information is constructed without sending HTTP requests.
    /// Samples scalar fields automatically. The x-verification-payloads, x-verification-provider-payloads, x-verification-dispatch,
    /// and x-verification-requests extensions supply explicit contracts. Projected tagged unions also exercise known and unknown tags.
    /// Returns findings without writing files. These probes are not exhaustive JSON Schema validation.
    /// </remarks>
    /// <param name="document">Normalized specification with any explicit verification contracts.</param>
    /// <param name="assembly">The compiled generated-client assembly.</param>
    /// <param name="options">Model namespace and API client naming conventions.</param>
    /// <param name="cancellationToken">Cancels between models and request contracts.</param>
    /// <returns>Numbers of models and payloads checked, with every collected failure.</returns>
    [RequiresUnreferencedCode("Runtime diagnostics inspect generated models and factories using reflection.")]
    [RequiresDynamicCode("Runtime diagnostics construct generic serialization probes for generated models.")]
    Task<GeneratedClientRuntimeReport> Analyze(JsonNode document, Assembly assembly, GeneratedClientRuntimeOptions? options = null,
        CancellationToken cancellationToken = default);
}
