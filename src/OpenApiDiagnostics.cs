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
using Soenneker.OpenApi.Diagnostics.Abstract;
using Soenneker.OpenApi.Diagnostics.Analyzers;
using Soenneker.OpenApi.Diagnostics.Analyzers.Abstract;
using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics;

/// <summary>
/// A highly comprehensive OpenAPI diagnostic tool, with a focus on detecting issues
/// that can cause problems for code generators like Kiota.
/// </summary>
/// <summary>
/// Service for analyzing OpenAPI documents and identifying potential issues
/// </summary>
public class OpenApiDiagnostics : IOpenApiDiagnostics
{
    private readonly OpenApiStringReader _reader;
    private readonly ISchemaAnalyzer _schemaAnalyzer;
    private readonly IPathAnalyzer _pathAnalyzer;

    private static readonly Regex PathParameterRegex = new(@"\{([^}]+)\}", RegexOptions.Compiled);
    private static readonly Regex ValidIdentifierRegex = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

    private static readonly HashSet<string> CsharpKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
        "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
        "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
        "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
        "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
        "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
    };

    public OpenApiDiagnostics(ISchemaAnalyzer schemaAnalyzer, IPathAnalyzer pathAnalyzer)
    {
        _reader = new OpenApiStringReader();
        _schemaAnalyzer = schemaAnalyzer;
        _pathAnalyzer = pathAnalyzer;
    }

    /// <summary>
    /// Analyzes an OpenAPI document from a JSON string
    /// </summary>
    public async Task<List<OpenApiDiagnosticIssue>> Analyze(string openApiJson)
    {
        var issues = new List<OpenApiDiagnosticIssue>();
        try
        {
            var document = _reader.Read(openApiJson, out var diagnostic);
            if (diagnostic.Errors.Any())
            {
                foreach (var error in diagnostic.Errors)
                {
                    issues.Add(new OpenApiDiagnosticIssue
                    {
                        Severity = DiagnosticSeverity.Error,
                        Category = DiagnosticCategory.Structure,
                        Code = "PARSE_ERROR",
                        Message = error.Message,
                        Location = error.Pointer
                    });
                }

                return issues;
            }

            return await AnalyzeDocument(document);
        }
        catch (Exception ex)
        {
            issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = DiagnosticSeverity.Error,
                Category = DiagnosticCategory.Structure,
                Code = "UNEXPECTED_ERROR",
                Message = $"Unexpected error: {ex.Message}",
                Location = ""
            });
            return issues;
        }
    }

    /// <summary>
    /// Analyzes an OpenAPI document from a file
    /// </summary>
    public async Task<List<OpenApiDiagnosticIssue>> AnalyzeFile(string file)
    {
        var json = await File.ReadAllTextAsync(file);
        return await Analyze(json);
    }

    /// <summary>
    /// Analyzes an OpenAPI document
    /// </summary>
    private async Task<List<OpenApiDiagnosticIssue>> AnalyzeDocument(OpenApiDocument document)
    {
        var issues = new List<OpenApiDiagnosticIssue>();
        try
        {
            await AnalyzeDocumentStructure(document, issues);
            await _schemaAnalyzer.AnalyzeSchemas(document, issues);
            await _pathAnalyzer.AnalyzePaths(document, issues);
            return issues;
        }
        catch (Exception ex)
        {
            issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = DiagnosticSeverity.Error,
                Category = DiagnosticCategory.Structure,
                Code = "UNEXPECTED_ERROR",
                Message = $"Unexpected error: {ex.Message}",
                Location = ""
            });
            return issues;
        }
    }

    /// <summary>
    /// Analyzes the basic structure of the OpenAPI document
    /// </summary>
    private async Task AnalyzeDocumentStructure(OpenApiDocument document, List<OpenApiDiagnosticIssue> issues)
    {
        if (string.IsNullOrEmpty(document.Info?.Version))
        {
            issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = DiagnosticSeverity.Error,
                Category = DiagnosticCategory.Structure,
                Code = "MISSING_VERSION",
                Message = "OpenAPI version is missing",
                Location = "info.version"
            });
        }

        if (string.IsNullOrEmpty(document.Info?.Title))
        {
            issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = DiagnosticSeverity.Warning,
                Category = DiagnosticCategory.Structure,
                Code = "MISSING_TITLE",
                Message = "API title is missing",
                Location = "info.title"
            });
        }

        if (string.IsNullOrEmpty(document.Info?.Description))
        {
            issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = DiagnosticSeverity.Warning,
                Category = DiagnosticCategory.Structure,
                Code = "MISSING_DESCRIPTION",
                Message = "API description is missing",
                Location = "info.description"
            });
        }

        if (document.Servers == null || !document.Servers.Any())
        {
            issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = DiagnosticSeverity.Warning,
                Category = DiagnosticCategory.Structure,
                Code = "NO_SERVERS",
                Message = "No servers defined in the OpenAPI document",
                Location = "servers"
            });
        }
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
        if (schema == null) return;

        // --- Dependency Graph Construction for Circular Reference Check ---
        // Use a temporary set to avoid modifying the context's collection while iterating
        var dependencies = new HashSet<string>();

        // Check properties
        if (schema.Properties != null)
        {
            foreach (var prop in schema.Properties.Values)
            {
                if (prop.Reference != null)
                {
                    dependencies.Add(prop.Reference.Id);
                }
            }
        }

        // Check array items
        if (schema.Items?.Reference != null)
        {
            dependencies.Add(schema.Items.Reference.Id);
        }

        // Check composition schemas (allOf, anyOf, oneOf)
        var compositionSchemas = (schema.AllOf ?? Enumerable.Empty<OpenApiSchema>()).Concat(schema.AnyOf ?? Enumerable.Empty<OpenApiSchema>())
                                                                                    .Concat(schema.OneOf ?? Enumerable.Empty<OpenApiSchema>());

        foreach (var compSchema in compositionSchemas)
        {
            if (compSchema.Reference != null)
            {
                dependencies.Add(compSchema.Reference.Id);
            }
        }

        context.SchemaDependencies[schemaName] = dependencies;
        // --- End of Dependency Graph Construction ---

        // --- KIOTA-SPECIFIC AND CRITICAL CHECKS ---

        // Check for untyped objects, which generate weak dictionaries
        if (schema.Type == "object" && (schema.Properties == null || !schema.Properties.Any()) && schema.AdditionalProperties != null)
            context.AddIssue(DiagnosticSeverity.Warning, DiagnosticCategory.Kiota, "UNTYPED_OBJECT_SCHEMA",
                $"Schema '{schemaName}' defines an untyped object (dictionary). This will generate a weakly-typed dictionary instead of a strong class.",
                location, schemaName, "schema");

        // Check discriminator validity (a major source of Kiota errors)
        if (schema.Discriminator != null)
        {
            var propName = schema.Discriminator.PropertyName;
            if (string.IsNullOrWhiteSpace(propName))
            {
                context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Schema, "DISCRIMINATOR_MISSING_PROPERTY_NAME",
                    $"Schema '{schemaName}' has a discriminator but it's missing a 'propertyName'.", location, schemaName, "schema");
            }
            // CRITICAL CHECK: The discriminator property MUST be in the 'required' list.
            else if (schema.Required == null || !schema.Required.Contains(propName))
            {
                context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Kiota, "DISCRIMINATOR_PROPERTY_NOT_REQUIRED",
                    $"The discriminator property '{propName}' must be in the 'required' list for the schema '{schemaName}'. Kiota will fail without this.",
                    location, schemaName, "schema");
            }
        }
        // Check for polymorphism without a discriminator
        else if (schema.OneOf != null && schema.OneOf.Any())
        {
            context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Kiota, "MISSING_DISCRIMINATOR",
                $"Schema '{schemaName}' uses 'oneOf' for polymorphism but is missing a 'discriminator' object, which is required by Kiota for code generation.",
                location, schemaName, "schema");
        }

        // Check for invalid property names (causes Kiota crashes)
        if (schema.Properties != null)
        {
            foreach (var property in schema.Properties)
            {
                if (string.IsNullOrWhiteSpace(property.Key))
                {
                    context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Kiota, "EMPTY_PROPERTY_NAME",
                        $"Schema '{schemaName}' contains a property with an empty name. This will cause code generators to crash.", $"{location}.properties",
                        schemaName, "schema");
                }
            }
        }

        // Check for invalid enum values (causes Kiota crashes)
        if (schema.Enum != null)
        {
            if (schema.Type == "string" && schema.Enum.OfType<OpenApiString>().Any(s => string.IsNullOrWhiteSpace(s.Value)))
            {
                context.AddIssue(DiagnosticSeverity.Error, DiagnosticCategory.Kiota, "EMPTY_ENUM_VALUE",
                    $"Enum in schema '{schemaName}' contains an empty or whitespace-only string value. This is invalid and will cause code generators to crash.",
                    location, schemaName, "schema");
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

    private async Task AnalyzeEnums(OpenApiDocument document, List<OpenApiDiagnosticIssue> issues)
    {
        foreach (var schema in document.Components.Schemas)
        {
            await AnalyzeSchemaEnums(schema.Key, schema.Value, issues);
        }
    }

    private async Task AnalyzeSchemaEnums(string schemaName, OpenApiSchema schema, List<OpenApiDiagnosticIssue> issues, string path = "")
    {
        if (schema == null) return;

        var currentPath = string.IsNullOrEmpty(path) ? schemaName : $"{path}.{schemaName}";

        // Check enum values
        if (schema.Enum != null && schema.Enum.Any())
        {
            // Check for empty enum arrays
            if (schema.Enum.Any(e => e is IList<object> list && list.Count == 0))
            {
                issues.Add(new OpenApiDiagnosticIssue
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.Schema,
                    Code = "EMPTY_ENUM_ARRAY",
                    Message = $"Schema '{currentPath}' contains an empty enum array value",
                    Location = $"components.schemas.{currentPath}.enum",
                    ComponentName = schemaName,
                    ComponentType = "schema"
                });
            }

            // Check for single-value enums
            if (schema.Enum.Count == 1)
            {
                var value = schema.Enum.First();
                var valueType = value?.GetType().Name ?? "null";

                issues.Add(new OpenApiDiagnosticIssue
                {
                    Severity = DiagnosticSeverity.Warning,
                    Category = DiagnosticCategory.Schema,
                    Code = "SINGLE_VALUE_ENUM",
                    Message = $"Schema '{currentPath}' has an enum with only one value ({valueType}: {value})",
                    Location = $"components.schemas.{currentPath}.enum",
                    ComponentName = schemaName,
                    ComponentType = "schema"
                });
            }

            // Check for boolean enums
            if (schema.Enum.All(e => e is bool))
            {
                issues.Add(new OpenApiDiagnosticIssue
                {
                    Severity = DiagnosticSeverity.Warning,
                    Category = DiagnosticCategory.Schema,
                    Code = "BOOLEAN_ENUM",
                    Message = $"Schema '{currentPath}' uses an enum for boolean values - consider using type: boolean instead",
                    Location = $"components.schemas.{currentPath}.enum",
                    ComponentName = schemaName,
                    ComponentType = "schema"
                });
            }

            // Check for nested arrays in enums
            if (schema.Enum.Any(e => e is IList<object>))
            {
                issues.Add(new OpenApiDiagnosticIssue
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.Schema,
                    Code = "NESTED_ARRAY_ENUM",
                    Message = $"Schema '{currentPath}' contains nested arrays in enum values",
                    Location = $"components.schemas.{currentPath}.enum",
                    ComponentName = schemaName,
                    ComponentType = "schema"
                });
            }

            // Check for mixed types in enums
            var types = schema.Enum.Select(e => e?.GetType().Name ?? "null").Distinct().ToList();
            if (types.Count > 1)
            {
                issues.Add(new OpenApiDiagnosticIssue
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.Schema,
                    Code = "MIXED_TYPE_ENUM",
                    Message = $"Schema '{currentPath}' has enum values of mixed types: {string.Join(", ", types)}",
                    Location = $"components.schemas.{currentPath}.enum",
                    ComponentName = schemaName,
                    ComponentType = "schema"
                });
            }
        }

        // Recursively check properties
        if (schema.Properties != null)
        {
            foreach (var property in schema.Properties)
            {
                await AnalyzeSchemaEnums(property.Key, property.Value, issues, currentPath);
            }
        }

        // Check items for array types
        if (schema.Items != null)
        {
            await AnalyzeSchemaEnums("items", schema.Items, issues, currentPath);
        }

        // Check allOf
        if (schema.AllOf != null)
        {
            for (int i = 0; i < schema.AllOf.Count; i++)
            {
                await AnalyzeSchemaEnums($"allOf[{i}]", schema.AllOf[i], issues, currentPath);
            }
        }

        // Check oneOf
        if (schema.OneOf != null)
        {
            for (int i = 0; i < schema.OneOf.Count; i++)
            {
                await AnalyzeSchemaEnums($"oneOf[{i}]", schema.OneOf[i], issues, currentPath);
            }
        }

        // Check anyOf
        if (schema.AnyOf != null)
        {
            for (int i = 0; i < schema.AnyOf.Count; i++)
            {
                await AnalyzeSchemaEnums($"anyOf[{i}]", schema.AnyOf[i], issues, currentPath);
            }
        }
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