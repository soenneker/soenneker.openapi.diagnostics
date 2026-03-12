using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.OpenApi;
using Soenneker.OpenApi.Diagnostics.Analyzers.Abstract;
using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics.Analyzers;

/// <summary>
/// Analyzes path definitions in OpenAPI documents
/// </summary>
public class PathAnalyzer : IPathAnalyzer
{
    /// <summary>
    /// Analyzes all paths in the document
    /// </summary>
    public async Task AnalyzePaths(OpenApiDocument document, List<OpenApiDiagnosticIssue> issues)
    {
        if (document.Paths == null || !document.Paths.Any())
        {
            issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = DiagnosticSeverity.Error,
                Category = DiagnosticCategory.Path,
                Code = "NO_PATHS",
                Message = "No paths defined in the OpenAPI document",
                Location = "paths"
            });
            return;
        }

        foreach (KeyValuePair<string, IOpenApiPathItem> path in document.Paths)
        {
            await AnalyzePath(path.Key, path.Value, issues);
        }
    }

    /// <summary>
    /// Analyzes a specific path and its operations
    /// </summary>
    private async Task AnalyzePath(string path, IOpenApiPathItem pathItem, List<OpenApiDiagnosticIssue> issues)
    {
        // Check path format
        if (!IsValidPathFormat(path))
        {
            issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = DiagnosticSeverity.Error,
                Category = DiagnosticCategory.Path,
                Code = "INVALID_PATH_FORMAT",
                Message = $"Path '{path}' has an invalid format",
                Location = $"paths.{path}",
                ComponentName = path,
                ComponentType = "path"
            });
        }

        // Check for duplicate parameters
        var parameters = pathItem.Parameters?.ToList() ?? new List<IOpenApiParameter>();
        var duplicateParams = parameters.GroupBy(p => p.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var paramName in duplicateParams)
        {
            issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = DiagnosticSeverity.Error,
                Category = DiagnosticCategory.Path,
                Code = "DUPLICATE_PARAMETER",
                Message = $"Path '{path}' has duplicate parameter '{paramName}'",
                Location = $"paths.{path}.parameters",
                ComponentName = path,
                ComponentType = "path"
            });
        }

        // Check operations
        if (!pathItem.Operations.Any())
        {
            issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = DiagnosticSeverity.Warning,
                Category = DiagnosticCategory.Path,
                Code = "NO_OPERATIONS",
                Message = $"Path '{path}' has no operations defined",
                Location = $"paths.{path}",
                ComponentName = path,
                ComponentType = "path"
            });
        }

        foreach (KeyValuePair<HttpMethod, OpenApiOperation> operation in pathItem.Operations)
        {
            await AnalyzeOperation(path, operation.Key, operation.Value, issues);
        }
    }

    /// <summary>
    /// Analyzes a specific operation
    /// </summary>
    private async Task AnalyzeOperation(string path, HttpMethod httpMethod, OpenApiOperation operation, List<OpenApiDiagnosticIssue> issues)
    {
        string methodString = httpMethod.ToString().ToLowerInvariant();
        // Check operation ID
        if (string.IsNullOrEmpty(operation.OperationId))
        {
            issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = DiagnosticSeverity.Warning,
                Category = DiagnosticCategory.Operation,
                Code = "MISSING_OPERATION_ID",
                Message = $"Operation {methodString} on path '{path}' is missing an operationId",
                Location = $"paths.{path}.{methodString}",
                ComponentName = path,
                ComponentType = "operation"
            });
        }
        else if (!IsValidOperationId(operation.OperationId))
        {
            issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = DiagnosticSeverity.Warning,
                Category = DiagnosticCategory.Operation,
                Code = "INVALID_OPERATION_ID",
                Message = $"Operation ID '{operation.OperationId}' contains invalid characters",
                Location = $"paths.{path}.{methodString}",
                ComponentName = path,
                ComponentType = "operation"
            });
        }

        // Check for duplicate parameters
        var parameters = operation.Parameters?.ToList() ?? new List<IOpenApiParameter>();
        var duplicateParams = parameters.GroupBy(p => p.Name)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var paramName in duplicateParams)
        {
            issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = DiagnosticSeverity.Error,
                Category = DiagnosticCategory.Operation,
                Code = "DUPLICATE_PARAMETER",
                Message = $"Operation {methodString} on path '{path}' has duplicate parameter '{paramName}'",
                Location = $"paths.{path}.{methodString}.parameters",
                ComponentName = path,
                ComponentType = "operation"
            });
        }

        // Check for required parameters
        foreach (var parameter in parameters)
        {
            // Remove .Nullable usage, as IOpenApiSchema does not have Nullable
            // If you need to check for nullability, use the Type property and check for JsonSchemaType.Null
        }

        // Check responses
        if (operation.Responses == null || !operation.Responses.Any())
        {
            issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = DiagnosticSeverity.Error,
                Category = DiagnosticCategory.Operation,
                Code = "NO_RESPONSES",
                Message = $"Operation {methodString} on path '{path}' has no responses defined",
                Location = $"paths.{path}.{methodString}",
                ComponentName = path,
                ComponentType = "operation"
            });
        }
        else if (!operation.Responses.ContainsKey("200") && !operation.Responses.ContainsKey("201") && !operation.Responses.ContainsKey("204"))
        {
            issues.Add(new OpenApiDiagnosticIssue
            {
                Severity = DiagnosticSeverity.Warning,
                Category = DiagnosticCategory.Operation,
                Code = "NO_SUCCESS_RESPONSE",
                Message = $"Operation {methodString} on path '{path}' has no success response (200, 201, or 204)",
                Location = $"paths.{path}.{methodString}.responses",
                ComponentName = path,
                ComponentType = "operation"
            });
        }
    }

    private bool IsValidPathFormat(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        if (!path.StartsWith("/")) return false;

        string[] segments = path.Split('/');
        foreach (string segment in segments)
        {
            if (string.IsNullOrEmpty(segment)) continue;

            // Check for valid path parameter format
            if (segment.StartsWith("{") && segment.EndsWith("}"))
            {
                string paramName = segment.Substring(1, segment.Length - 2);
                if (string.IsNullOrEmpty(paramName)) return false;
                if (paramName.Contains("/")) return false;
            }
            else if (segment.Contains("{") || segment.Contains("}"))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsValidOperationId(string operationId)
    {
        if (string.IsNullOrEmpty(operationId)) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(operationId, "^[a-zA-Z][a-zA-Z0-9_]*$");
    }
} 