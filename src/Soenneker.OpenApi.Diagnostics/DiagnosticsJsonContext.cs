using System.Text.Json.Serialization;
using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics;

/// <summary>Source-generated camel-case JSON serialization for diagnostic reports and document issues.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(GeneratedCodeDiagnosticReport))]
[JsonSerializable(typeof(GeneratedClientRuntimeReport))]
[JsonSerializable(typeof(KiotaDiagnosticReport))]
[JsonSerializable(typeof(List<OpenApiDiagnosticIssue>))]
public partial class DiagnosticsJsonContext : JsonSerializerContext;
