using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.OpenApi.Diagnostics.Abstract;
using Soenneker.OpenApi.Diagnostics.Analyzers;
using Soenneker.OpenApi.Diagnostics.Analyzers.Abstract;
using Soenneker.Utils.File.Registrars;

namespace Soenneker.OpenApi.Diagnostics.Registrars;

/// <summary>
/// Registrar for OpenAPI diagnostics services
/// </summary>
public static class OpenApiDiagnosticsRegistrar
{
    /// <summary>
    /// Registers the OpenAPI diagnostics services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddOpenApiDiagnostics(this IServiceCollection services)
    {
        services.AddFileUtilAsSingleton();
        services.TryAddSingleton<IEnumAnalyzer, EnumAnalyzer>();
        services.TryAddSingleton<ISchemaAnalyzer, SchemaAnalyzer>();
        services.TryAddSingleton<IPathAnalyzer, PathAnalyzer>();
        services.TryAddSingleton<IOpenApiDiagnostics, OpenApiDiagnostics>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IOpenApiDiagnostics"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddOpenApiDiagnosticsAsScoped(this IServiceCollection services)
    {
        services.AddFileUtilAsScoped();
        services.TryAddScoped<IEnumAnalyzer, EnumAnalyzer>();
        services.TryAddScoped<ISchemaAnalyzer, SchemaAnalyzer>();
        services.TryAddScoped<IPathAnalyzer, PathAnalyzer>();
        services.TryAddScoped<IOpenApiDiagnostics, OpenApiDiagnostics>();

        return services;
    }
}