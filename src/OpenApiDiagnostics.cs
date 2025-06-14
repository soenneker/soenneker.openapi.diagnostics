using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace Soenneker.OpenApi.Diagnostics;

/// <summary>
/// A highly comprehensive OpenAPI diagnostic tool, with a focus on detecting issues
/// that can cause problems for code generators like Kiota.
/// </summary>
public interface IOpenApiDiagnostics
{
    /// <summary>
    //Analyzes an OpenAPI document from a JSON string.
    //</summary>
    /// <param name="openApiJson">The OpenAPI content as a JSON string.</param>
    /// <returns>A list of diagnostic issues found in the document.</returns>
    Task<List<OpenApiDiagnosticIssue>> Analyze(string openApiJson);

    /// <summary>
    //Analyzes an OpenAPI document from a stream.
    //</summary>
    /// <param name="openApiStream">The stream containing the OpenAPI document.</param>
    /// <returns>A list of diagnostic issues found in the document.</returns>
    Task<List<OpenApiDiagnosticIssue>> Analyze(Stream openApiStream);

    /// <summary>
    //Analyzes an OpenAPI document from a file.
    // </summary>
    /// <param name="fileInfo">A FileInfo object pointing to the OpenAPI document.</param>
    /// <returns>A list of diagnostic issues found in the document.</returns>
    Task<List<OpenApiDiagnosticIssue>> Analyze(FileInfo fileInfo);

    /// <summary>
    // Analyzes a pre-parsed OpenAPI document object.
    // </summary>
    /// <param name="document">The OpenApiDocument object.</param>
    /// <returns>A list of diagnostic issues found in the document.</returns>
    List<OpenApiDiagnosticIssue> Analyze(OpenApiDocument document);

    Task<List<OpenApiDiagnosticIssue>> AnalyzeFile(string file);
}

/// <summary>
/// A class representing a single issue found during OpenAPI document analysis.
/// </summary>
public class OpenApiDiagnosticIssue
{
    public DiagnosticSeverity Severity { get; set; }
    public DiagnosticCategory Category { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }

    /// <summary>
    /// A JSON pointer-like location of the issue (e.g., "paths./users/{id}.get.operationId").
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// The path of the component, if applicable (e.g., "/users/{id}").
    /// </summary>
    public string ComponentPath { get; set; }

    /// <summary>
    /// The name of the component, if applicable (e.g., "GetUserById").
    /// </summary>
    public string ComponentName { get; set; }

    /// <summary>
    /// The type of the component, if applicable (e.g., "schema", "operation").
    /// </summary>
    public string ComponentType { get; set; }
}

public enum DiagnosticCategory
{
    Structure,
    Schema,
    Path,
    Operation,
    Parameter,
    Response,
    Security,
    Naming,
    Kiota,
    Other
}

public enum DiagnosticSeverity
{
    Error,
    Warning,
    Info
}

/// <inheritdoc cref="IOpenApiDiagnostics"/>
public class OpenApiDiagnostics : IOpenApiDiagnostics
{
    private static readonly Regex ValidIdentifierRegex = new(@"^[a-zA-Z][a-zA-Z0-9_]*$", RegexOptions.Compiled);
    private static readonly Regex PathParameterRegex = new(@"\{([^\}]+)\}", RegexOptions.Compiled);

    private static readonly HashSet<string> CsharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
        "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected",
        "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while"
    };

    /// <inheritdoc />
    public async Task<List<OpenApiDiagnosticIssue>> Analyze(string openApiJson)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(openApiJson));
        return await Analyze(stream);
    }

    public async Task<List<OpenApiDiagnosticIssue>> AnalyzeFile(string file)
    {
        var data = File.OpenRead(file);
        return await Analyze(data);
    }

    /// <inheritdoc />
    public async Task<List<OpenApiDiagnosticIssue>> Analyze(Stream openApiStream)
    {
        var reader = new OpenApiStreamReader();
        var result = await reader.ReadAsync(openApiStream);

        if (result.OpenApiDiagnostic.Errors.Any())
        {
            return result.OpenApiDiagnostic.Errors.Select(error => new OpenApiDiagnosticIssue
                         {
                             Severity = DiagnosticSeverity.Error,
                             Category = DiagnosticCategory.Structure,
                             Code = "PARSE_ERROR",
                             Message = error.Message,
                             Location = error.Pointer
                         })
                         .ToList();
        }

        return Analyze(result.OpenApiDocument);
    }

    /// <inheritdoc />
    public async Task<List<OpenApiDiagnosticIssue>> Analyze(FileInfo fileInfo)
    {
        if (!fileInfo.Exists)
        {
            return new List<OpenApiDiagnosticIssue>
            {
                new()
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.Structure,
                    Code = "FILE_NOT_FOUND",
                    Message = $"File not found: {fileInfo.FullName}"
                }
            };
        }

        await using var stream = fileInfo.OpenRead();
        return await Analyze(stream);
    }

    /// <inheritdoc />
    public List<OpenApiDiagnosticIssue> Analyze(OpenApiDocument document)
    {
        try
        {
            var context = new AnalysisContext(document);
            AnalyzeDocument(context);
            return context.Issues;
        }
        catch (Exception ex)
        {
            return new List<OpenApiDiagnosticIssue>
            {
                new()
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.Structure,
                    Code = "UNEXPECTED_ANALYSIS_ERROR",
                    Message = $"An unexpected error occurred during analysis: {ex.Message}",
                }
            };
        }
    }

    /// <summary>
    /// The main analysis orchestrator.
    /// </summary>
    private void AnalyzeDocument(AnalysisContext context)
    {
        AnalyzeInfo(context);
        AnalyzeServers(context);

        if (context.Document.Paths == null || !context.Document.Paths.Any())
        {
            context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Structure, "MISSING_PATHS", "The document must contain at least one path.", "paths");
        }
        else
        {
            AnalyzePathsAndOperations(context);
        }

        if (context.Document.Components != null)
        {
            AnalyzeComponents(context);
        }

        AnalyzeSecurity(context);
        AnalyzeTags(context);

        // These analyses depend on the full component graph being built first.
        AnalyzeCircularDependencies(context);
    }

    private void AnalyzeInfo(AnalysisContext context)
    {
        if (context.Document.Info == null)
        {
            context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Structure, "MISSING_INFO", "The 'info' object is required.", "info");
            return;
        }

        if (string.IsNullOrWhiteSpace(context.Document.Info.Title))
            context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Structure, "MISSING_TITLE", "Info object must have a 'title'.", "info.title");

        if (string.IsNullOrWhiteSpace(context.Document.Info.Version))
            context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Structure, "MISSING_VERSION", "Info object must have a 'version'.", "info.version");
    }

    private void AnalyzeServers(AnalysisContext context)
    {
        if (context.Document.Servers == null || !context.Document.Servers.Any())
        {
            context.AddIssue(DiagnosticSeverity.Warning, DiagnosticCategory.Structure, "MISSING_SERVERS",
                "No 'servers' are defined. Clients may not know how to connect.", "servers");
        }
    }

    private void AnalyzePathsAndOperations(AnalysisContext context)
    {
        foreach (var path in context.Document.Paths)
        {
            string pathKey = path.Key;
            var pathItem = path.Value;
            string pathLocation = $"paths.{pathKey}";

            if (!pathKey.StartsWith("/"))
                context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Path, "INVALID_PATH_START", "Path must start with a '/' character.",
                    pathLocation);

            var pathPlaceholders = PathParameterRegex.Matches(pathKey).Cast<Match>().Select(m => m.Groups[1].Value).ToHashSet();

            var pathLevelParams = pathItem.Parameters.Select(p => p.Name).ToHashSet();

            foreach (var operation in pathItem.Operations)
            {
                var op = operation.Value;
                string opType = operation.Key.ToString().ToLowerInvariant();
                string opLocation = $"{pathLocation}.{opType}";

                AnalyzeOperation(context, op, opType, pathKey, opLocation);

                // Validate path parameters are correctly defined for the operation
                var allParams = op.Parameters.ToDictionary(p => (p.Name, p.In), p => p);
                foreach (var pathParam in pathItem.Parameters)
                {
                    allParams.TryAdd((pathParam.Name, pathParam.In), pathParam);
                }

                var definedPathParams = allParams.Values.Where(p => p.In == ParameterLocation.Path).Select(p => p.Name).ToHashSet();

                var missingParams = pathPlaceholders.Except(definedPathParams);
                foreach (var missing in missingParams)
                    context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Path, "MISSING_PATH_PARAMETER",
                        $"Path '{pathKey}' specifies placeholder '{{{missing}}}' but it is not defined as a path parameter for the operation.", opLocation,
                        componentName: op.OperationId);

                var extraParams = definedPathParams.Except(pathPlaceholders);
                foreach (var extra in extraParams)
                    context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Path, "UNDEFINED_PATH_PARAMETER",
                        $"Operation defines path parameter '{extra}' but it is not present as a placeholder in the path '{pathKey}'.",
                        $"{opLocation}.parameters", componentName: op.OperationId);
            }
        }
    }

    private void AnalyzeOperation(AnalysisContext context, OpenApiOperation op, string opType, string path, string opLocation)
    {
        // OperationId
        if (string.IsNullOrWhiteSpace(op.OperationId))
        {
            context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Kiota, "MISSING_OPERATION_ID",
                "OperationId is missing. This is required for most code generators.", opLocation);
        }
        else
        {
            if (!ValidIdentifierRegex.IsMatch(op.OperationId))
                context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Kiota, "INVALID_OPERATION_ID",
                    $"OperationId '{op.OperationId}' contains invalid characters. Kiota requires it to be a valid method name (letters, numbers, underscores, starting with a letter).",
                    opLocation);

            if (!context.OperationIds.Add(op.OperationId))
                context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Kiota, "DUPLICATE_OPERATION_ID",
                    $"Duplicate OperationId '{op.OperationId}'. All operationIds must be unique.", opLocation);
        }

        // Parameters
        AnalyzeParameters(context, op.Parameters, op.OperationId, opLocation);

        // Request Body
        if (op.RequestBody != null)
            AnalyzeContent(context, op.RequestBody.Content, $"{opLocation}.requestBody.content", op.OperationId, isRequest: true);

        // Responses
        AnalyzeResponses(context, op.Responses, op.OperationId, opLocation);
    }

    private void AnalyzeParameters(AnalysisContext context, IList<OpenApiParameter> parameters, string operationId, string location)
    {
        if (parameters == null) return;

        var seenParams = new HashSet<(string Name, ParameterLocation In)>();
        foreach (var parameter in parameters)
        {
            if (parameter.Reference != null) continue; // Assume referenced parameters are valid.

            if (string.IsNullOrWhiteSpace(parameter.Name))
            {
                context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Parameter, "MISSING_PARAMETER_NAME", "Parameter is missing a 'name'.", location,
                    operationId);
                continue;
            }

            if (!seenParams.Add((parameter.Name, parameter.In.GetValueOrDefault())))
            {
                context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Parameter, "DUPLICATE_PARAMETER",
                    $"Duplicate parameter found with name '{parameter.Name}' and location '{parameter.In}'.", location, operationId);
            }

            if (parameter.In == ParameterLocation.Path && !parameter.Required)
                context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Parameter, "PATH_PARAM_NOT_REQUIRED",
                    $"Path parameter '{parameter.Name}' must be marked as required.", $"{location}.parameters", operationId);

            if (parameter.Schema == null)
                context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Parameter, "MISSING_PARAMETER_SCHEMA",
                    $"Parameter '{parameter.Name}' is missing a 'schema'.", $"{location}.parameters", operationId);
            else if (parameter.Schema.Reference == null && parameter.Schema.Type == "object" && parameter.Schema.Properties != null &&
                     parameter.Schema.Properties.Any())
                context.AddIssue(DiagnosticSeverity.Warning, DiagnosticCategory.Kiota, "INLINE_COMPLEX_PARAMETER_SCHEMA",
                    $"Parameter '{parameter.Name}' uses a complex inline schema. For best code generation results, define this as a reusable component in '#/components/schemas'.",
                    $"{location}.parameters", operationId);
        }
    }

    private void AnalyzeResponses(AnalysisContext context, OpenApiResponses responses, string operationId, string opLocation)
    {
        if (responses == null || !responses.Any())
        {
            context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Response, "MISSING_RESPONSES", "Operation must define at least one response.",
                opLocation, operationId);
            return;
        }

        var successCodes = responses.Keys.Count(k => k.StartsWith("2"));
        if (successCodes == 0)
            context.AddIssue(DiagnosticSeverity.Warning, DiagnosticCategory.Response, "NO_SUCCESS_RESPONSE",
                "Operation does not define any 2xx success responses.", opLocation, operationId);

        if (successCodes > 1)
            context.AddIssue(DiagnosticSeverity.Warning, DiagnosticCategory.Kiota, "MULTIPLE_SUCCESS_RESPONSES",
                "Operation defines multiple 2xx success responses. This can lead to ambiguous, weakly-typed return types (e.g., object or union types) in generated code.",
                opLocation, operationId);

        foreach (var response in responses)
        {
            var responseLocation = $"{opLocation}.responses.{response.Key}";
            AnalyzeContent(context, response.Value.Content, $"{responseLocation}.content", operationId, isRequest: false);
        }
    }

    private void AnalyzeContent(AnalysisContext context, IDictionary<string, OpenApiMediaType> content, string location, string operationId, bool isRequest)
    {
        if (content == null || !content.Any())
        {
            if (isRequest)
                context.AddIssue(DiagnosticSeverity.Warning, DiagnosticCategory.Structure, "EMPTY_REQUEST_BODY_CONTENT",
                    "Request body is defined but has no content types specified.", location, operationId);
            return;
        }

        foreach (var mediaType in content)
        {
            var mediaTypeLocation = $"{location}.{mediaType.Key}";
            var schema = mediaType.Value.Schema;

            if (schema == null)
            {
                context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Schema, "MISSING_SCHEMA_IN_CONTENT",
                    $"Content type '{mediaType.Key}' is missing a schema.", mediaTypeLocation, operationId);
                continue;
            }

            // Kiota check for inline complex schemas
            if (schema.Reference == null && schema.Type == "object" && schema.Properties != null && schema.Properties.Any())
                context.AddIssue(DiagnosticSeverity.Warning, DiagnosticCategory.Kiota, "INLINE_COMPLEX_SCHEMA",
                    "A complex schema is defined inline. For better, reusable generated code, define this in '#/components/schemas' and use a $ref.",
                    mediaTypeLocation, operationId);

            // Kiota check for binary formats
            if (schema.Type == "string" && (schema.Format == "binary" || schema.Format == "byte") && mediaType.Key.ToLowerInvariant().Contains("json"))
                context.AddIssue(DiagnosticSeverity.Warning, DiagnosticCategory.Kiota, "BINARY_IN_JSON",
                    $"The schema indicates binary content ('{schema.Format}'), but the content type is '{mediaType.Key}'. Consider using 'application/octet-stream' or another binary-friendly media type.",
                    mediaTypeLocation, operationId);
        }
    }

    private void AnalyzeComponents(AnalysisContext context)
    {
        var schemas = context.Document.Components?.Schemas;
        if (schemas == null) return;

        var normalizedSchemaNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var schema in schemas)
        {
            string name = schema.Key;
            var schemaDef = schema.Value;
            string location = $"#/components/schemas/{name}";

            if (!ValidIdentifierRegex.IsMatch(name))
                context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Naming, "INVALID_SCHEMA_NAME",
                    $"Schema name '{name}' is not a valid identifier (should be letters, numbers, underscores, starting with a letter).", location, name,
                    "schema");

            if (CsharpKeywords.Contains(name.ToLowerInvariant()))
                context.AddIssue(DiagnosticSeverity.Warning, DiagnosticCategory.Kiota, "CSHARP_KEYWORD_SCHEMA_NAME",
                    $"Schema name '{name}' is a C# reserved keyword, which may cause issues during code generation.", location, name, "schema");

            string normalized = SanitizeAndConvertToPascalCase(name);
            if (normalizedSchemaNames.TryGetValue(normalized, out var existingName))
            {
                context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Kiota, "NORMALIZED_NAME_COLLISION",
                    $"Schema name '{name}' results in the name '{normalized}' after normalization, which conflicts with schema '{existingName}'. This will cause a type name collision in generated code.",
                    location, name, "schema");
            }
            else
            {
                normalizedSchemaNames[normalized] = name;
            }

            AnalyzeSchema(context, schemaDef, name, location);
        }
    }

    private void AnalyzeSchema(AnalysisContext context, OpenApiSchema schema, string schemaName, string location)
    {
        // Build dependency graph for circular reference check
        context.SchemaDependencies[schemaName] = new HashSet<string>();
        foreach (var referencedSchema in schema.Properties.Values.Concat(schema.Items != null ? new[] {schema.Items} : Enumerable.Empty<OpenApiSchema>()))
        {
            if (referencedSchema.Reference != null)
            {
                var refName = referencedSchema.Reference.Id;
                context.SchemaDependencies[schemaName].Add(refName);
            }
        }

        // Kiota check for untyped objects
        if (schema.Type == "object" && (schema.Properties == null || !schema.Properties.Any()) &&
            (schema.AdditionalPropertiesAllowed || schema.AdditionalProperties != null))
            context.AddIssue(DiagnosticSeverity.Warning, DiagnosticCategory.Kiota, "UNTYPED_OBJECT_SCHEMA",
                $"Schema '{schemaName}' defines an untyped object (dictionary). This will generate a weakly-typed dictionary instead of a strong class.",
                location, schemaName, "schema");

        // Check for polymorphism without discriminator
        if ((schema.OneOf != null && schema.OneOf.Any()) && schema.Discriminator == null)
            context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Kiota, "MISSING_DISCRIMINATOR",
                $"Schema '{schemaName}' uses 'oneOf' for polymorphism but is missing a 'discriminator' object, which is required by Kiota.", location,
                schemaName, "schema");

        if (schema.Enum != null)
        {
            if (schema.Type == "string" && schema.Enum.OfType<OpenApiString>().Any(s => string.IsNullOrWhiteSpace(s.Value)))
            {
                context.AddIssue(DiagnosticSeverity.Warning, DiagnosticCategory.Schema, "EMPTY_ENUM_VALUE",
                    $"Enum in schema '{schemaName}' contains an empty or whitespace-only string value.", location, schemaName, "schema");
            }
        }
    }

    private void AnalyzeCircularDependencies(AnalysisContext context)
    {
        var nodes = context.SchemaDependencies.Keys.ToList();
        foreach (var node in nodes)
        {
            var path = new List<string>();
            FindCycles(node, path, context);
        }
    }

    private void FindCycles(string currentNode, List<string> path, AnalysisContext context)
    {
        path.Add(currentNode);

        if (context.SchemaDependencies.TryGetValue(currentNode, out var dependencies))
        {
            foreach (var dependency in dependencies)
            {
                int cycleStartIndex = path.IndexOf(dependency);
                if (cycleStartIndex != -1)
                {
                    var cycle = path.GetRange(cycleStartIndex, path.Count - cycleStartIndex);
                    cycle.Add(dependency); // Close the loop

                    // Create a canonical key for the cycle to report it only once
                    var canonicalKey = string.Join("->", cycle.Distinct().OrderBy(n => n));
                    if (context.ReportedCycles.Add(canonicalKey))
                    {
                        string cyclePath = string.Join(" -> ", cycle);
                        context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Schema, "CIRCULAR_DEPENDENCY",
                            $"Circular dependency detected: {cyclePath}. This can cause stack overflows or un-generatable code.",
                            $"#/components/schemas/{dependency}", dependency, "schema");
                    }
                }
                else
                {
                    FindCycles(dependency, path, context);
                }
            }
        }

        path.RemoveAt(path.Count - 1);
    }

    private void AnalyzeSecurity(AnalysisContext context)
    {
        if (context.Document.Components?.SecuritySchemes == null || !context.Document.Components.SecuritySchemes.Any()) return;

        foreach (var scheme in context.Document.Components.SecuritySchemes)
        {
            if (scheme.Value.Type == SecuritySchemeType.OAuth2 && scheme.Value.Flows == null)
            {
                context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Security, "MISSING_OAUTH_FLOWS",
                    $"OAuth2 security scheme '{scheme.Key}' must define 'flows'.", $"#/components/securitySchemes/{scheme.Key}");
            }
        }
    }

    private void AnalyzeTags(AnalysisContext context)
    {
        if (context.Document.Tags == null) return;

        var definedTags = new HashSet<string>(context.Document.Tags.Select(t => t.Name), StringComparer.Ordinal);
        if (!definedTags.Any()) return;

        foreach (var op in context.Document.Paths.Values.SelectMany(p => p.Operations.Values))
        {
            if (op.Tags == null || !op.Tags.Any())
            {
                context.AddIssue(DiagnosticSeverity.Info, DiagnosticCategory.Other, "OPERATION_UNTAGGED",
                    $"Operation '{op.OperationId}' is not associated with any tags.", $"paths", op.OperationId);
                continue;
            }

            foreach (var tag in op.Tags)
            {
                if (!definedTags.Contains(tag.Name))
                {
                    context.AddIssue(DiagnosticSeverity.Warning, DiagnosticCategory.Other, "UNDEFINED_TAG",
                        $"Operation '{op.OperationId}' uses tag '{tag.Name}' which is not defined in the global tags list.", $"paths", op.OperationId);
                }
            }
        }
    }

    /// <summary>
    /// A robust method to sanitize a string and convert it to PascalCase.
    /// It handles various separators like hyphens, underscores, and spaces.
    /// </summary>
    private static string SanitizeAndConvertToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;

        // Split by non-alphanumeric characters
        var parts = Regex.Split(name, @"[^\w]").Where(p => !string.IsNullOrEmpty(p)).ToList();

        if (!parts.Any()) return string.Empty;

        var pascalCaseBuilder = new StringBuilder();
        foreach (var part in parts)
        {
            pascalCaseBuilder.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1)
            {
                pascalCaseBuilder.Append(part.Substring(1));
            }
        }

        return pascalCaseBuilder.ToString();
    }


    /// <summary>
    /// Holds the state for a single analysis run to ensure the main class is stateless.
    /// </summary>
    private class AnalysisContext
    {
        public OpenApiDocument Document { get; }
        public List<OpenApiDiagnosticIssue> Issues { get; } = new();
        public HashSet<string> OperationIds { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, HashSet<string>> SchemaDependencies { get; } = new();
        public HashSet<string> ReportedCycles { get; } = new();

        public AnalysisContext(OpenApiDocument document)
        {
            Document = document;
        }

        public void AddIssue(DiagnosticSeverity severity, DiagnosticCategory category, string code, string message, string location,
            string componentName = null, string componentPath = null, string componentType = null)
        {
            Issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = severity,
                Category = category,
                Code = code,
                Message = message,
                Location = location,
                ComponentName = componentName,
                ComponentPath = componentPath,
                ComponentType = componentType
            });
        }
    }
}