using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.OpenApi;
using Soenneker.OpenApi.Diagnostics.Analyzers.Abstract;
using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics.Analyzers;

/// <summary>
/// Analyzes enum definitions in OpenAPI documents
/// </summary>
public class EnumAnalyzer : IEnumAnalyzer
{
    /// <summary>
    /// Analyzes all enums in the document
    /// </summary>
    public async Task AnalyzeEnums(OpenApiDocument document, List<OpenApiDiagnosticIssue> issues)
    {
        var visited = new HashSet<string>();
        foreach (var schema in document.Components.Schemas)
        {
            await AnalyzeSchemaEnums(schema.Key, schema.Value, issues, visited);
        }
    }

    /// <summary>
    /// Analyzes enums in a specific schema and its nested schemas
    /// </summary>
    private async Task AnalyzeSchemaEnums(string schemaName, IOpenApiSchema schema, List<OpenApiDiagnosticIssue> issues, HashSet<string> visited, string path = "")
    {
        if (schema == null) return;

        string currentPath = string.IsNullOrEmpty(path) ? schemaName : $"{path}.{schemaName}";
        
        // Prevent infinite recursion by tracking visited schemas
        if (schema is OpenApiSchemaReference refSchema)
        {
            var refPath = refSchema.Id;
            if (!visited.Add(refPath))
            {
                return; // Skip if we've already visited this schema
            }
        }

        // Check enum values
        if (schema.Enum != null && schema.Enum.Any())
        {
            // Check for empty enum arrays
            if (schema.Enum.Any(e => e is IList<object> list && list.Count == 0))
            {
                issues.Add(new OpenApiDiagnosticIssue
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.Enum,
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
                string? valueType = value?.GetType().Name ?? "null";
                
                issues.Add(new OpenApiDiagnosticIssue
                {
                    Severity = DiagnosticSeverity.Warning,
                    Category = DiagnosticCategory.Enum,
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
                    Category = DiagnosticCategory.Enum,
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
                    Category = DiagnosticCategory.Enum,
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
                    Category = DiagnosticCategory.Enum,
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
                await AnalyzeSchemaEnums(property.Key, property.Value, issues, visited, currentPath);
            }
        }

        // Check items for array types
        if (schema.Items != null)
        {
            await AnalyzeSchemaEnums("items", schema.Items, issues, visited, currentPath);
        }

        // Check allOf
        if (schema.AllOf != null)
        {
            for (int i = 0; i < schema.AllOf.Count; i++)
            {
                await AnalyzeSchemaEnums($"allOf[{i}]", schema.AllOf[i], issues, visited, currentPath);
            }
        }

        // Check oneOf
        if (schema.OneOf != null)
        {
            for (int i = 0; i < schema.OneOf.Count; i++)
            {
                await AnalyzeSchemaEnums($"oneOf[{i}]", schema.OneOf[i], issues, visited, currentPath);
            }
        }

        // Check anyOf
        if (schema.AnyOf != null)
        {
            for (int i = 0; i < schema.AnyOf.Count; i++)
            {
                await AnalyzeSchemaEnums($"anyOf[{i}]", schema.AnyOf[i], issues, visited, currentPath);
            }
        }
    }
} 