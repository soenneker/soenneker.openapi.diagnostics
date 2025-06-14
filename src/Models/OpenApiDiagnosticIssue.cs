using System;

namespace Soenneker.OpenApi.Diagnostics.Models;

/// <summary>
/// Represents a diagnostic issue found in an OpenAPI document
/// </summary>
public class OpenApiDiagnosticIssue
{
    /// <summary>
    /// The severity level of the issue
    /// </summary>
    public DiagnosticSeverity Severity { get; set; }

    /// <summary>
    /// The category of the issue
    /// </summary>
    public DiagnosticCategory Category { get; set; }

    /// <summary>
    /// A unique code identifying the type of issue
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable message describing the issue
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The location in the document where the issue was found
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Additional details about the issue
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// The path to the component where the issue was found
    /// </summary>
    public string? ComponentPath { get; set; }

    /// <summary>
    /// The name of the component where the issue was found
    /// </summary>
    public string? ComponentName { get; set; }

    /// <summary>
    /// The type of component where the issue was found
    /// </summary>
    public string? ComponentType { get; set; }
}

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

/// <summary>
/// Represents the category of a diagnostic issue
/// </summary>
public enum DiagnosticCategory
{
    /// <summary>
    /// Issues related to operation IDs
    /// </summary>
    OperationId,

    /// <summary>
    /// Issues related to references
    /// </summary>
    Reference,

    /// <summary>
    /// Issues related to enums
    /// </summary>
    Enum,

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