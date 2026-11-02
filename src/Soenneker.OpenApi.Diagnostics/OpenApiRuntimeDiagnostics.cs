using Soenneker.OpenApi.Diagnostics.Abstract;
using Soenneker.OpenApi.Diagnostics.Models;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Serialization.Json;

namespace Soenneker.OpenApi.Diagnostics;

public sealed class OpenApiRuntimeDiagnostics : IOpenApiRuntimeDiagnostics
{
    [RequiresUnreferencedCode("Runtime diagnostics inspect generated models and factories using reflection.")]
    [RequiresDynamicCode("Runtime diagnostics construct generic serialization probes for generated models.")]
    public async Task<GeneratedClientRuntimeReport> Analyze(JsonNode document, Assembly assembly, GeneratedClientRuntimeOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(assembly);
        options ??= new GeneratedClientRuntimeOptions();
        cancellationToken.ThrowIfCancellationRequested();
        var failures = new List<string>();
        int models = 0, payloads = 0, sampledModels = 0;
        var exercisedContracts = new HashSet<string>();
        foreach (Type type in assembly.GetTypes().Where(t => (options.ModelsNamespaceSuffix == null || t.Namespace?.EndsWith(options.ModelsNamespaceSuffix, StringComparison.Ordinal) == true) &&
            !t.IsAbstract && typeof(IParsable).IsAssignableFrom(t) && !t.IsNested))
        {
            if (document["components"]?["schemas"]?[type.Name] is not JsonObject schema) continue;
            cancellationToken.ThrowIfCancellationRequested();
            models++;
            try
            {
                var model = (IParsable)Activator.CreateInstance(type)!;
                HashSet<string> fields = FieldNames(model, new HashSet<Type>());
                foreach (string field in schema["x-client-raw-error-schema"] == null ? Properties(schema, document, new HashSet<string>()) : [])
                    if (!fields.Contains(field)) failures.Add(type.Name + ": missing deserializer for " + field);
                if (schema["x-client-raw-error-schema"] == null && model is not IComposedTypeWrapper &&
                    ScalarSample(schema, document, new HashSet<string>()) is { Count: > 0 } sample)
                {
                    await (Task)typeof(OpenApiRuntimeDiagnostics).GetMethod(nameof(RoundTrip), BindingFlags.NonPublic | BindingFlags.Static)!
                        .MakeGenericMethod(type).Invoke(null, [sample.ToJsonString(), false])!;
                    sampledModels++;
                    payloads++;
                }
                if (schema["x-verification-payloads"] is JsonArray examples)
                    foreach (JsonNode? example in examples)
                    {
                        await (Task)typeof(OpenApiRuntimeDiagnostics).GetMethod(nameof(RoundTrip), BindingFlags.NonPublic | BindingFlags.Static)!
                            .MakeGenericMethod(type).Invoke(null, [example!.ToJsonString(), true])!;
                        payloads++;
                    }
                if (schema["x-verification-dispatch"] is JsonArray dispatchCases)
                    foreach (JsonObject example in dispatchCases.OfType<JsonObject>())
                    {
                        await (Task)typeof(OpenApiRuntimeDiagnostics).GetMethod(nameof(Dispatch), BindingFlags.NonPublic | BindingFlags.Static)!
                            .MakeGenericMethod(type).Invoke(null, [example["payload"]!.ToJsonString(), example["type"]!.GetValue<string>()])!;
                        payloads++;
                    }
                if (schema["x-client-original-tagged-union"] != null && schema["discriminator"] is JsonObject discriminator &&
                    discriminator["mapping"] is JsonObject mappings)
                {
                    string tag = discriminator["propertyName"]!.GetValue<string>();
                    foreach ((string label, JsonNode? reference) in mappings)
                    {
                        string targetName = reference!.GetValue<string>()[21..].Replace("~1", "/").Replace("~0", "~");
                        JsonObject target = document["components"]!["schemas"]![targetName]!.AsObject();
                        JsonObject payload = ScalarSample(target, document, new HashSet<string>());
                        payload[tag] = label;
                        await (Task)typeof(OpenApiRuntimeDiagnostics).GetMethod(nameof(Dispatch), BindingFlags.NonPublic | BindingFlags.Static)!
                            .MakeGenericMethod(type).Invoke(null, [payload.ToJsonString(), targetName])!;
                        payloads++;
                    }
                    string unknown = "__verification_unknown_tag__";
                    while (mappings.ContainsKey(unknown)) unknown += "_";
                    var future = new JsonObject { [tag] = unknown, ["future_payload"] = new JsonObject { ["nested"] = new JsonArray(1, null, "x") } };
                    await (Task)typeof(OpenApiRuntimeDiagnostics).GetMethod(nameof(Dispatch), BindingFlags.NonPublic | BindingFlags.Static)!
                        .MakeGenericMethod(type).Invoke(null, [future.ToJsonString(), type.Name])!;
                    payloads++;
                }
                if (schema["x-verification-provider-payloads"] is JsonArray providerExamples)
                {
                    exercisedContracts.Add(type.Name);
                    foreach (JsonNode? example in providerExamples)
                    {
                        await (Task)typeof(OpenApiRuntimeDiagnostics).GetMethod(nameof(RoundTrip), BindingFlags.NonPublic | BindingFlags.Static)!
                            .MakeGenericMethod(type).Invoke(null, [example!.ToJsonString(), false])!;
                        payloads++;
                    }
                }
            }
            catch (Exception error) when (error is not OperationCanceledException) { failures.Add(type.Name + ": " + error.GetBaseException().Message); }
        }
        if (document["components"]?["schemas"] is JsonObject schemas)
            foreach ((string name, JsonNode? schema) in schemas)
                if (schema?["x-verification-provider-payloads"] != null && !exercisedContracts.Contains(name))
                    failures.Add(name + ": required provider contract was not exercised");
        int requests = 0;
        try
        {
            if (document["x-verification-requests"] is JsonArray { Count: > 0 })
            {
                requests = await GeneratedRequestDiagnostics.Run(document, assembly, options.ClientTypeName, cancellationToken);
            }
        }
        catch (Exception error) when (error is not OperationCanceledException) { failures.Add("Request contract: " + error.GetBaseException().Message); }
        cancellationToken.ThrowIfCancellationRequested();
        return new GeneratedClientRuntimeReport
        {
            Models = models, Payloads = payloads, SampledModels = sampledModels, Requests = requests, Failures = failures
        };
    }

    // Exercise actual field readers and writers, not just the presence of their dictionary keys.
    // Keep these probes to scalar fields and scalar collections: arbitrary union/object samples
    // would require a complete JSON Schema validator to avoid testing an invalid branch.
    private static JsonObject ScalarSample(JsonObject schema, JsonNode document, HashSet<string> seen)
    {
        var sample = new JsonObject();
        if (schema["$ref"] is JsonValue reference && reference.TryGetValue(out string? path) && seen.Add(path) &&
            path.StartsWith("#/components/schemas/", StringComparison.Ordinal) &&
            document["components"]?["schemas"]?[path[21..].Replace("~1", "/").Replace("~0", "~")] is JsonObject target)
            sample = ScalarSample(target, document, seen);
        if (schema["allOf"] is JsonArray all)
            foreach (JsonObject branch in all.OfType<JsonObject>())
                foreach ((string name, JsonNode? value) in ScalarSample(branch, document, seen)) sample[name] = value?.DeepClone();
        if (schema["properties"] is JsonObject properties)
            foreach ((string name, JsonNode? field) in properties)
            {
                // An allOf redeclaration also inherits the base field's constraints (e.g. date-time).
                // Skip automatic samples for overlaps; dedicated contracts cover those intersections.
                if (schema["allOf"] is JsonArray parents && parents.OfType<JsonObject>()
                    .Any(parent => Properties(parent, document, new HashSet<string>()).Contains(name)))
                {
                    sample.Remove(name);
                    continue;
                }
                if (TryScalar(field, document, new HashSet<string>(), out JsonNode? value)) sample[name] = value;
            }
        return sample;
    }

    private static bool TryScalar(JsonNode? node, JsonNode document, HashSet<string> seen, out JsonNode? value)
    {
        value = null;
        if (node is not JsonObject schema) return false;
        if (schema["readOnly"]?.ToJsonString() == "true") return false;
        if (schema["$ref"] is JsonValue reference && reference.TryGetValue(out string? path))
            return seen.Add(path) && path.StartsWith("#/components/schemas/", StringComparison.Ordinal) &&
                TryScalar(document["components"]?["schemas"]?[path[21..].Replace("~1", "/").Replace("~0", "~")], document, seen, out value);
        if (schema.ContainsKey("oneOf") || schema.ContainsKey("anyOf") || schema.ContainsKey("allOf") || schema.ContainsKey("not")) return false;
        string? kind = schema["type"] is JsonArray types ? types.OfType<JsonValue>().Select(t => t.GetValue<string>()).FirstOrDefault(t => t != "null") : schema["type"]?.GetValue<string>();
        string? format = schema["format"]?.GetValue<string>().ToLowerInvariant();
        // Temporal codecs can normalize offsets/precision while retaining the same instant.
        if (format is "date" or "date-time" or "time" or "duration" or "binary") return false;
        if (schema["enum"] is JsonArray values)
        {
            value = values.FirstOrDefault(v => v is JsonValue scalar && (kind switch
            {
                "string" => scalar.TryGetValue<string>(out _),
                "boolean" => scalar.TryGetValue<bool>(out _),
                "integer" or "number" => scalar.TryGetValue<decimal>(out _),
                _ => false
            }))?.DeepClone();
            return value != null;
        }
        if (kind == "array")
        {
            if (!TryScalar(schema["items"], document, seen, out JsonNode? item) || item is JsonArray) return false;
            value = new JsonArray(item);
            return true;
        }
        if (kind == "integer" || schema["x-client-original-type"]?.ToJsonString().Contains("integer", StringComparison.Ordinal) == true)
            foreach (string bound in new[] { "maximum", "minimum" })
                if (schema[bound] is JsonValue limit && limit.TryGetValue(out decimal number) && (number > int.MaxValue || number < int.MinValue))
                {
                    value = JsonValue.Create(bound == "maximum" ? decimal.Floor(number) : decimal.Ceiling(number));
                    return true;
                }
        value = kind switch
        {
            "string" => JsonValue.Create(format is "byte" or "base64url" ? "AQID" : format == "uuid" ? "1dbe8894-181d-4f52-94f4-8da754d53f51" : "wire-value"),
            "integer" => JsonValue.Create(format == "int64" ? 9007199254740993L : 17L),
            "number" => JsonValue.Create(format is "int8" or "uint8" or "int16" or "uint16" or "int32" or "int64" ? 17m : 1.25m),
            "boolean" => JsonValue.Create(true),
            _ => null
        };
        return value != null;
    }

    [RequiresUnreferencedCode("Runtime diagnostics inspect generated models and factories using reflection.")]
    [RequiresDynamicCode("Runtime diagnostics construct generic serialization probes for generated models.")]
    private static HashSet<string> FieldNames(IParsable model, HashSet<Type> seen)
    {
        var names = new HashSet<string>(model.GetFieldDeserializers().Keys);
        if (!seen.Add(model.GetType()) || model is not IComposedTypeWrapper) return names;
        foreach (PropertyInfo property in model.GetType().GetProperties().Where(p => typeof(IParsable).IsAssignableFrom(p.PropertyType) &&
            !p.PropertyType.IsAbstract && p.PropertyType.GetConstructor(Type.EmptyTypes) != null))
            names.UnionWith(FieldNames((IParsable)Activator.CreateInstance(property.PropertyType)!, seen));
        return names;
    }

    private static IEnumerable<string> Properties(JsonObject schema, JsonNode document, HashSet<string> seen)
    {
        if (schema["$ref"] is JsonValue reference)
        {
            string path = reference.GetValue<string>();
            if (path.StartsWith("#/components/schemas/") && seen.Add(path) &&
                document["components"]?["schemas"]?[path[21..].Replace("~1", "/").Replace("~0", "~")] is JsonObject target)
                foreach (string name in Properties(target, document, seen)) yield return name;
        }
        if (schema["properties"] is JsonObject properties)
            foreach (string name in properties.Select(p => p.Key)) yield return name;
        if (schema["allOf"] is JsonArray all)
            foreach (JsonObject item in all.OfType<JsonObject>())
                foreach (string name in Properties(item, document, seen)) yield return name;
    }

    [RequiresUnreferencedCode("Runtime diagnostics inspect generated models and factories using reflection.")]
    [RequiresDynamicCode("Runtime diagnostics construct generic serialization probes for generated models.")]
    private static async Task Dispatch<T>(string json, string expectedType) where T : IParsable
    {
        var factory = (ParsableFactory<T>)typeof(T).GetMethod("CreateFromDiscriminatorValue", BindingFlags.Static | BindingFlags.Public)!
            .CreateDelegate(typeof(ParsableFactory<T>));
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(json));
        IParseNode node = await new JsonParseNodeFactory().GetRootParseNodeAsync("application/json", input);
        T? value = node.GetObjectValue(factory);
        string[] expected = expectedType.Split('.', 2);
        if (value?.GetType().Name != expected[0])
            throw new InvalidOperationException($"Expected {expectedType} dispatch, got {value?.GetType().Name} for {json}");
        if (expected.Length == 2 && value!.GetType().GetProperty(expected[1])?.GetValue(value) == null)
            throw new InvalidOperationException($"Expected selected member {expectedType} for {json}");
        await RoundTrip<T>(json, false);
    }

    [RequiresUnreferencedCode("Runtime diagnostics inspect generated models and factories using reflection.")]
    [RequiresDynamicCode("Runtime diagnostics construct generic serialization probes for generated models.")]
    private static async Task RoundTrip<T>(string json, bool exact) where T : IParsable
    {
        var factory = (ParsableFactory<T>)typeof(T).GetMethod("CreateFromDiscriminatorValue", BindingFlags.Static | BindingFlags.Public)!
            .CreateDelegate(typeof(ParsableFactory<T>));
        using var input = new MemoryStream(Encoding.UTF8.GetBytes(json));
        IParseNode node = await new JsonParseNodeFactory().GetRootParseNodeAsync("application/json", input);
        T? value = node.GetObjectValue(factory);
        using var writer = new JsonSerializationWriter();
        writer.WriteObjectValue(null, value);
        using Stream output = writer.GetSerializedContent();
        JsonNode? actual = await JsonNode.ParseAsync(output);
        JsonNode? expected = JsonNode.Parse(json);
        if (exact ? !JsonNode.DeepEquals(expected, actual) : !Preserves(expected, actual))
            throw new InvalidOperationException($"Payload changed: {json} -> {actual}");
    }

    private static bool Preserves(JsonNode? expected, JsonNode? actual)
    {
        if (expected is JsonObject fields)
            return actual is JsonObject result && fields.All(p => result.ContainsKey(p.Key) && Preserves(p.Value, result[p.Key]));
        if (expected is JsonArray values)
            return actual is JsonArray result && values.Count == result.Count && values.Select((value, index) => Preserves(value, result[index])).All(v => v);
        return JsonNode.DeepEquals(expected, actual);
    }
}
