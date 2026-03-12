using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.OpenApi;
using Soenneker.OpenApi.Diagnostics.Analyzers.Abstract;
using Soenneker.OpenApi.Diagnostics.Models;
using System;

namespace Soenneker.OpenApi.Diagnostics.Analyzers;

/// <summary>
/// Analyzes schema definitions in OpenAPI documents
/// </summary>
public class SchemaAnalyzer : ISchemaAnalyzer
{
    private readonly IEnumAnalyzer _enumAnalyzer;

    public SchemaAnalyzer(IEnumAnalyzer enumAnalyzer)
    {
        _enumAnalyzer = enumAnalyzer;
    }

    /// <summary>
    /// Analyzes all schemas in the document
    /// </summary>
    public async Task AnalyzeSchemas(OpenApiDocument document, List<OpenApiDiagnosticIssue> issues)
    {
        // First analyze enums
        await _enumAnalyzer.AnalyzeEnums(document, issues);

        var visited = new HashSet<string>();
        // Then analyze other schema aspects
        foreach (KeyValuePair<string, IOpenApiSchema> schema in document.Components.Schemas)
        {
            await AnalyzeSchema(schema.Key, schema.Value, issues, visited);
        }
    }

    /// <summary>
    /// Analyzes a specific schema and its nested schemas
    /// </summary>
    private async Task AnalyzeSchema(string schemaName, IOpenApiSchema schema, List<OpenApiDiagnosticIssue> issues, HashSet<string> visited, string path = "")
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

        // Check for empty inline schemas
        if (schema.Properties != null)
        {
            foreach (KeyValuePair<string, IOpenApiSchema> property in schema.Properties)
            {
                if (property.Value == null || (property.Value.Properties == null && property.Value.Type == null))
                {
                    issues.Add(new OpenApiDiagnosticIssue
                    {
                        Severity = DiagnosticSeverity.Error,
                        Category = DiagnosticCategory.Schema,
                        Code = "EMPTY_INLINE_SCHEMA",
                        Message = $"Property '{property.Key}' in schema '{currentPath}' has an empty inline schema",
                        Location = $"components.schemas.{currentPath}.properties.{property.Key}",
                        ComponentName = schemaName,
                        ComponentType = "schema"
                    });
                }
            }
        }

        // Check for empty array items
        if (schema.Type == JsonSchemaType.Array && (schema.Items == null || (schema.Items.Properties == null && schema.Items.Type == null)))
        {
            issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = DiagnosticSeverity.Error,
                Category = DiagnosticCategory.Schema,
                Code = "EMPTY_ARRAY_ITEMS",
                Message = $"Array schema '{currentPath}' has empty items definition",
                Location = $"components.schemas.{currentPath}.items",
                ComponentName = schemaName,
                ComponentType = "schema"
            });
        }

        // Check for missing discriminator mappings
        if (schema.Discriminator != null)
        {
            // Check if discriminator property is in required list
            if (string.IsNullOrWhiteSpace(schema.Discriminator.PropertyName))
            {
                issues.Add(new OpenApiDiagnosticIssue
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.Schema,
                    Code = "DISCRIMINATOR_MISSING_PROPERTY_NAME",
                    Message = $"Schema '{currentPath}' has a discriminator but it's missing a 'propertyName'",
                    Location = $"components.schemas.{currentPath}.discriminator",
                    ComponentName = schemaName,
                    ComponentType = "schema"
                });
            }
            else if (schema.Required == null || !schema.Required.Contains(schema.Discriminator.PropertyName))
            {
                issues.Add(new OpenApiDiagnosticIssue
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.Kiota,
                    Code = "DISCRIMINATOR_PROPERTY_NOT_REQUIRED",
                    Message = $"The discriminator property '{schema.Discriminator.PropertyName}' must be in the 'required' list for the schema '{currentPath}'. Code generators like Kiota will fail without this.",
                    Location = $"components.schemas.{currentPath}",
                    ComponentName = schemaName,
                    ComponentType = "schema"
                });
            }

            if (schema.Discriminator.Mapping == null || !schema.Discriminator.Mapping.Any())
            {
                issues.Add(new OpenApiDiagnosticIssue
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.Schema,
                    Code = "MISSING_DISCRIMINATOR_MAPPING",
                    Message = $"Schema '{currentPath}' has a discriminator but no mapping defined",
                    Location = $"components.schemas.{currentPath}.discriminator",
                    ComponentName = schemaName,
                    ComponentType = "schema"
                });
            }
            else
            {
                // Check if all mapped schemas exist
                foreach (KeyValuePair<string, OpenApiSchemaReference> mapping in schema.Discriminator.Mapping)
                {
                    var refPath = mapping.Value.Id;
                    if (!refPath.StartsWith("#/components/schemas/"))
                    {
                        issues.Add(new OpenApiDiagnosticIssue
                        {
                            Severity = DiagnosticSeverity.Error,
                            Category = DiagnosticCategory.Schema,
                            Code = "INVALID_DISCRIMINATOR_MAPPING",
                            Message = $"Discriminator mapping in schema '{currentPath}' has invalid reference path: {refPath}",
                            Location = $"components.schemas.{currentPath}.discriminator.mapping.{mapping.Key}",
                            ComponentName = schemaName,
                            ComponentType = "schema"
                        });
                    }
                }
            }
        }
        else if (schema.OneOf != null && schema.OneOf.Any())
        {
            issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = DiagnosticSeverity.Error,
                Category = DiagnosticCategory.Kiota,
                Code = "MISSING_DISCRIMINATOR",
                Message = $"Schema '{currentPath}' uses 'oneOf' for polymorphism but is missing a 'discriminator' object, which is required by Kiota for code generation.",
                Location = $"components.schemas.{currentPath}",
                ComponentName = schemaName,
                ComponentType = "schema"
            });
        }

        // Check allOf fragments for missing type
        if (schema.AllOf != null)
        {
            for (int i = 0; i < schema.AllOf.Count; i++)
            {
                IOpenApiSchema fragment = schema.AllOf[i];
                if (fragment.Type == null)
                {
                    issues.Add(new OpenApiDiagnosticIssue
                    {
                        Severity = DiagnosticSeverity.Error,
                        Category = DiagnosticCategory.Schema,
                        Code = "MISSING_ALL_OF_TYPE",
                        Message = $"allOf fragment at index {i} in schema '{currentPath}' is missing type definition",
                        Location = $"components.schemas.{currentPath}.allOf[{i}]",
                        ComponentName = schemaName,
                        ComponentType = "schema"
                    });
                }
                else if (fragment.Type != JsonSchemaType.Object)
                {
                    issues.Add(new OpenApiDiagnosticIssue
                    {
                        Severity = DiagnosticSeverity.Warning,
                        Category = DiagnosticCategory.Schema,
                        Code = "NON_OBJECT_ALL_OF_TYPE",
                        Message = $"allOf fragment at index {i} in schema '{currentPath}' has type '{fragment.Type}' instead of 'object'",
                        Location = $"components.schemas.{currentPath}.allOf[{i}]",
                        ComponentName = schemaName,
                        ComponentType = "schema"
                    });
                }
            }
        }

        // Check for circular references
        if (schema is OpenApiSchemaReference refSchema2)
        {
            var refPath = refSchema2.Id;
            if (refPath == currentPath)
            {
                issues.Add(new OpenApiDiagnosticIssue
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.Schema,
                    Code = "CIRCULAR_REFERENCE",
                    Message = $"Schema '{currentPath}' has a circular reference to itself",
                    Location = $"components.schemas.{currentPath}",
                    ComponentName = schemaName,
                    ComponentType = "schema"
                });
            }
        }

        // Check for invalid type combinations
        if (schema.Type != null)
        {
            // Check for invalid type and format combinations
            if (schema.Type == JsonSchemaType.String && schema.Format == "date-time" && !IsValidDateTimeFormat(schema))
            {
                issues.Add(new OpenApiDiagnosticIssue
                {
                    Severity = DiagnosticSeverity.Warning,
                    Category = DiagnosticCategory.Schema,
                    Code = "INVALID_DATETIME_FORMAT",
                    Message = $"Schema '{currentPath}' has an invalid date-time format",
                    Location = $"components.schemas.{currentPath}",
                    ComponentName = schemaName,
                    ComponentType = "schema"
                });
            }

            // Check for invalid number formats
            if (schema.Type == JsonSchemaType.Number && schema.Format != null && !IsValidNumberFormat(schema.Format))
            {
                issues.Add(new OpenApiDiagnosticIssue
                {
                    Severity = DiagnosticSeverity.Warning,
                    Category = DiagnosticCategory.Schema,
                    Code = "INVALID_NUMBER_FORMAT",
                    Message = $"Schema '{currentPath}' has an invalid number format: {schema.Format}",
                    Location = $"components.schemas.{currentPath}",
                    ComponentName = schemaName,
                    ComponentType = "schema"
                });
            }
        }

        // Check for invalid constraints
        if (schema.Minimum != null && schema.Maximum != null)
        {
            int? minInt = null, maxInt = null;
            decimal? minDec = null, maxDec = null;
            try { minInt = Convert.ToInt32(schema.Minimum); } catch { }
            try { maxInt = Convert.ToInt32(schema.Maximum); } catch { }
            try { minDec = Convert.ToDecimal(schema.Minimum); } catch { }
            try { maxDec = Convert.ToDecimal(schema.Maximum); } catch { }
            if (minInt.HasValue && maxInt.HasValue && minInt > maxInt)
            {
                issues.Add(new OpenApiDiagnosticIssue
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.Schema,
                    Code = "INVALID_RANGE",
                    Message = $"Schema '{currentPath}' has minimum ({schema.Minimum}) greater than maximum ({schema.Maximum})",
                    Location = $"components.schemas.{currentPath}",
                    ComponentName = schemaName,
                    ComponentType = "schema"
                });
            }
            if (minDec.HasValue && maxDec.HasValue && minDec > maxDec)
            {
                issues.Add(new OpenApiDiagnosticIssue
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.Schema,
                    Code = "INVALID_RANGE",
                    Message = $"Schema '{currentPath}' has minimum ({schema.Minimum}) greater than maximum ({schema.Maximum})",
                    Location = $"components.schemas.{currentPath}",
                    ComponentName = schemaName,
                    ComponentType = "schema"
                });
            }
        }

        if (schema.MinLength != null && schema.MaxLength != null)
        {
            int? minLenInt = null, maxLenInt = null;
            try { minLenInt = Convert.ToInt32(schema.MinLength); } catch { }
            try { maxLenInt = Convert.ToInt32(schema.MaxLength); } catch { }
            if (minLenInt.HasValue && maxLenInt.HasValue && minLenInt > maxLenInt)
            {
                issues.Add(new OpenApiDiagnosticIssue
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.Schema,
                    Code = "INVALID_LENGTH_RANGE",
                    Message = $"Schema '{currentPath}' has minLength ({schema.MinLength}) greater than maxLength ({schema.MaxLength})",
                    Location = $"components.schemas.{currentPath}",
                    ComponentName = schemaName,
                    ComponentType = "schema"
                });
            }
        }

        if (schema.MinItems != null && schema.MaxItems != null)
        {
            int? minItemsInt = null, maxItemsInt = null;
            try { minItemsInt = Convert.ToInt32(schema.MinItems); } catch { }
            try { maxItemsInt = Convert.ToInt32(schema.MaxItems); } catch { }
            if (minItemsInt.HasValue && maxItemsInt.HasValue && minItemsInt > maxItemsInt)
            {
                issues.Add(new OpenApiDiagnosticIssue
                {
                    Severity = DiagnosticSeverity.Error,
                    Category = DiagnosticCategory.Schema,
                    Code = "INVALID_ITEMS_RANGE",
                    Message = $"Schema '{currentPath}' has minItems ({schema.MinItems}) greater than maxItems ({schema.MaxItems})",
                    Location = $"components.schemas.{currentPath}",
                    ComponentName = schemaName,
                    ComponentType = "schema"
                });
            }
        }

        // Check for invalid pattern
        if (!string.IsNullOrEmpty(schema.Pattern) && !IsValidRegex(schema.Pattern))
        {
            issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = DiagnosticSeverity.Error,
                Category = DiagnosticCategory.Schema,
                Code = "INVALID_PATTERN",
                Message = $"Schema '{currentPath}' has an invalid regex pattern",
                Location = $"components.schemas.{currentPath}",
                ComponentName = schemaName,
                ComponentType = "schema"
            });
        }

        // Recursively analyze properties
        if (schema.Properties != null)
        {
            foreach (KeyValuePair<string, IOpenApiSchema> property in schema.Properties)
            {
                await AnalyzeSchema(property.Key, property.Value, issues, visited, currentPath);
            }
        }

        // Analyze array items
        if (schema.Items != null)
        {
            await AnalyzeSchema("items", schema.Items, issues, visited, currentPath);
        }

        // Analyze allOf
        if (schema.AllOf != null)
        {
            for (int i = 0; i < schema.AllOf.Count; i++)
            {
                await AnalyzeSchema($"allOf[{i}]", schema.AllOf[i], issues, visited, currentPath);
            }
        }

        // Analyze oneOf
        if (schema.OneOf != null)
        {
            for (int i = 0; i < schema.OneOf.Count; i++)
            {
                await AnalyzeSchema($"oneOf[{i}]", schema.OneOf[i], issues, visited, currentPath);
            }
        }

        // Analyze anyOf
        if (schema.AnyOf != null)
        {
            for (int i = 0; i < schema.AnyOf.Count; i++)
            {
                await AnalyzeSchema($"anyOf[{i}]", schema.AnyOf[i], issues, visited, currentPath);
            }
        }
    }

    private bool IsValidDateTimeFormat(IOpenApiSchema schema)
    {
        return schema.Format == "date-time" || schema.Format == "date";
    }

    private bool IsValidNumberFormat(string format)
    {
        return format == "float" || format == "double" || format == "int32" || format == "int64";
    }

    private bool IsValidRegex(string pattern)
    {
        try
        {
            _ = new System.Text.RegularExpressions.Regex(pattern);
            return true;
        }
        catch
        {
            return false;
        }
    }
} 