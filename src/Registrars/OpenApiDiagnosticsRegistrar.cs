using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.OpenApi.Diagnostics.Abstract;

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
        services.AddSingleton<IOpenApiDiagnostics, OpenApiDiagnostics>();
        return services;
    }

    /// <summary>
    /// Adds <see cref="IOpenApiDiagnostics"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddOpenApiDiagnosticsAsScoped(this IServiceCollection services)
    {
        services.TryAddScoped<IOpenApiDiagnostics, OpenApiDiagnostics>();

        return services;
    }
}
