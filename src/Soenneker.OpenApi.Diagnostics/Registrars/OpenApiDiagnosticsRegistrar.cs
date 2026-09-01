using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.OpenApi.Diagnostics.Abstract;
using Soenneker.Utils.File.Registrars;
using Soenneker.Utils.MemoryStream.Registrars;

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
        services.AddMemoryStreamUtilAsSingleton();
        services.TryAddSingleton<IOpenApiDiagnostics, OpenApiDiagnostics>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IOpenApiDiagnostics"/> as a scoped service. <para/>
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddOpenApiDiagnosticsAsScoped(this IServiceCollection services)
    {
        services.AddFileUtilAsScoped();
        services.AddMemoryStreamUtilAsScoped();
        services.TryAddScoped<IOpenApiDiagnostics, OpenApiDiagnostics>();

        return services;
    }
}
