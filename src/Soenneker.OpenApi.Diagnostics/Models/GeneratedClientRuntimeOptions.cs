namespace Soenneker.OpenApi.Diagnostics.Models;

/// <summary>Naming conventions used to discover models and the API client in a generated assembly.</summary>
public sealed class GeneratedClientRuntimeOptions
{
    /// <summary>Namespace suffix selecting model types; null includes every namespace. Defaults to .Models.</summary>
    public string? ModelsNamespaceSuffix { get; init; } = ".Models";

    /// <summary>API client type name used to initialize serializers for request contracts. Defaults to ApiClient.</summary>
    public string ClientTypeName { get; init; } = "ApiClient";
}
