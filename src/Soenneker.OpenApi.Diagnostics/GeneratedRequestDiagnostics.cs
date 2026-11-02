using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Serialization.Json;

namespace Soenneker.OpenApi.Diagnostics;

[RequiresUnreferencedCode("Request diagnostics inspect generated request builders using reflection.")]
[RequiresDynamicCode("Request diagnostics construct generic model readers and query configuration delegates.")]
internal static class GeneratedRequestDiagnostics
{
    public static async Task<int> Run(JsonNode document, Assembly assembly, string clientTypeName, CancellationToken cancellationToken)
    {
        if (document["x-verification-requests"] is not JsonArray contracts) return 0;
        using var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider());
        adapter.BaseUrl = "https://example.invalid";
        // Construction registers the same serializers as a consuming application. No requests are sent.
        Type client = assembly.GetTypes().Single(t => t.Name == clientTypeName);
        ConstructorInfo clientConstructor = client.GetConstructors().First(c => c.GetParameters().FirstOrDefault()?.ParameterType == typeof(IRequestAdapter));
        clientConstructor.Invoke(clientConstructor.GetParameters().Select((p, i) => i == 0 ? (object)adapter : p.HasDefaultValue ? p.DefaultValue : null).ToArray());
        foreach (JsonObject contract in contracts.OfType<JsonObject>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            string route = contract["route"]!.GetValue<string>();
            string verb = contract["method"]!.GetValue<string>();
            object? builder = null;
            MethodInfo? method = null;
            foreach (Type type in assembly.GetTypes().Where(t => t.Name.EndsWith("RequestBuilder", StringComparison.Ordinal)))
            {
                MethodInfo? candidate = type.GetMethod("To" + verb + "RequestInformation");
                ConstructorInfo? constructor = type.GetConstructor([typeof(Dictionary<string, object>), typeof(IRequestAdapter)]);
                if (candidate == null || constructor == null) continue;
                object instance = constructor.Invoke([new Dictionary<string, object> { ["baseurl"] = adapter.BaseUrl }, adapter]);
                string? template = (string?)typeof(BaseRequestBuilder).GetProperty("UrlTemplate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(instance);
                string actualRoute = template!.Replace("{+baseurl}", "", StringComparison.Ordinal);
                int query = actualRoute.IndexOf("{?", StringComparison.Ordinal);
                if (query >= 0) actualRoute = actualRoute[..query];
                if (Uri.UnescapeDataString(actualRoute) != route) continue;
                if (builder != null) throw new InvalidOperationException("Ambiguous request contract route: " + route);
                builder = instance;
                method = candidate;
            }
            if (builder == null || method == null) throw new InvalidOperationException("Missing request builder for " + route);
            object?[] arguments = new object?[method.GetParameters().Length];
            for (int index = 0; index < arguments.Length; index++)
            {
                ParameterInfo parameter = method.GetParameters()[index];
                if (parameter.Name == "body" && contract["body"] is JsonNode body)
                    arguments[index] = parameter.ParameterType == typeof(string) ? body.GetValue<string>() :
                        typeof(GeneratedRequestDiagnostics).GetMethod(nameof(Parse), BindingFlags.Static | BindingFlags.NonPublic)!
                            .MakeGenericMethod(parameter.ParameterType).Invoke(null, [body.ToJsonString()]);
                else if (parameter.Name == "requestConfiguration" && contract["query"] is JsonObject query)
                    arguments[index] = typeof(GeneratedRequestDiagnostics).GetMethod(nameof(Configure), BindingFlags.Static | BindingFlags.NonPublic)!
                        .MakeGenericMethod(parameter.ParameterType.GetGenericArguments()[0]).Invoke(null, [query]);
                else arguments[index] = parameter.HasDefaultValue ? parameter.DefaultValue : null;
            }
            var request = (RequestInformation)method.Invoke(builder, arguments)!;
            if (contract["contentType"] is JsonValue contentType &&
                (!request.Headers.TryGetValue("Content-Type", out IEnumerable<string>? headers) || !headers.Any(h => h.StartsWith(contentType.GetValue<string>(), StringComparison.Ordinal))))
                throw new InvalidOperationException(route + ": wrong Content-Type");
            if (contract["form"] is JsonObject expectedForm)
            {
                using var reader = new StreamReader(request.Content!, Encoding.UTF8, leaveOpen: true);
                string text = await reader.ReadToEndAsync(cancellationToken);
                var fields = text.Split('&').Select(part => part.Split('=', 2)).ToDictionary(p => Decode(p[0]), p => p.Length == 2 ? Decode(p[1]) : "");
                foreach ((string key, JsonNode? value) in expectedForm)
                    if (!fields.TryGetValue(key, out string? actual) || actual != value!.GetValue<string>())
                        throw new InvalidOperationException(route + ": form field changed: " + key + "; body=" + text);
            }
            if (contract["queryContains"] is JsonArray expectedQuery)
                foreach (JsonValue value in expectedQuery.OfType<JsonValue>())
                    if (!Decode(request.URI.Query).Contains(value.GetValue<string>(), StringComparison.Ordinal))
                        throw new InvalidOperationException(route + ": query changed: " + request.URI);
        }
        return contracts.Count;
    }

    private static string Decode(string text) => Uri.UnescapeDataString(text.Replace("+", " ", StringComparison.Ordinal));

    private static T? Parse<T>(string json) where T : IParsable
    {
        var factory = (ParsableFactory<T>)typeof(T).GetMethod("CreateFromDiscriminatorValue", BindingFlags.Static | BindingFlags.Public)!.CreateDelegate(typeof(ParsableFactory<T>));
        using var document = System.Text.Json.JsonDocument.Parse(json);
        return new JsonParseNode(document.RootElement).GetObjectValue(factory);
    }

    private static Action<T> Configure<T>(JsonObject values) => configuration =>
    {
        object query = typeof(T).GetProperty("QueryParameters")!.GetValue(configuration)!;
        foreach ((string name, JsonNode? value) in values)
        {
            PropertyInfo property = query.GetType().GetProperties().Single(p => p.GetCustomAttribute<QueryParameterAttribute>()?.TemplateName == name || p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            property.SetValue(query, System.Text.Json.JsonSerializer.Deserialize(value!.ToJsonString(), property.PropertyType));
        }
    };
}
