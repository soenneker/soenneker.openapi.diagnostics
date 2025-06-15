namespace Soenneker.OpenApi.Diagnostics.Models;

/// <summary>
/// Represents the category of a diagnostic issue
/// </summary>
public enum DiagnosticCategory
{
    Naming,

    Kiota,

    /// <summary>
    /// Issues related to operation IDs
    /// </summary>
    OperationId,

    /// <summary>
    /// Issues related to operations
    /// </summary>
    Operation,

    /// <summary>
    /// Issues related to parameters
    /// </summary>
    Parameter,

    /// <summary>
    /// Issues related to security
    /// </summary>
    Security,

    Structure,

    /// <summary>
    /// Issues related to references
    /// </summary>
    Reference,

    /// <summary>
    /// Issues related to enums
    /// </summary>
    Enum,
    Schema,
    Response,

    /// <summary>
    /// Issues related to schema naming
    /// </summary>
    SchemaNaming,

    /// <summary>
    /// Issues related to polymorphic types
    /// </summary>
    PolymorphicType,

    /// <summary>
    /// Issues related to path parameters
    /// </summary>
    PathParameter,

    /// <summary>
    /// Issues related to paths
    /// </summary>
    Path,

    /// <summary>
    /// Issues related to recursive models
    /// </summary>
    RecursiveModel,

    /// <summary>
    /// Issues related to default values
    /// </summary>
    DefaultValue,

    /// <summary>
    /// Issues related to empty schemas
    /// </summary>
    EmptySchema,

    /// <summary>
    /// Issues related to schema combinations
    /// </summary>
    SchemaCombination,

    /// <summary>
    /// Issues related to format/type combinations
    /// </summary>
    FormatType,

    /// <summary>
    /// Issues related to OpenAPI version
    /// </summary>
    Version,

    /// <summary>
    /// Issues related to discriminator mapping
    /// </summary>
    Discriminator,

    /// <summary>
    /// Issues related to descriptions
    /// </summary>
    Description,

    /// <summary>
    /// Issues related to media types
    /// </summary>
    MediaType,

    /// <summary>
    /// Issues related to request bodies
    /// </summary>
    RequestBody,

    /// <summary>
    /// Issues related to file size
    /// </summary>
    FileSize,

    /// <summary>
    /// Issues related to empty keys
    /// </summary>
    EmptyKey,

    /// <summary>
    /// Issues related to nullable properties
    /// </summary>
    Nullable,

    /// <summary>
    /// Other types of issues
    /// </summary>
    Other
}