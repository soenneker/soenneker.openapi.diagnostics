using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;

namespace Soenneker.OpenApi.Diagnostics.Tests;

public sealed class GeneratedCodeDiagnosticsTests
{
    [Test]
    public async Task Reports_scalar_loss_nullable_enum_leaks_and_missing_typed_responses(CancellationToken cancellationToken)
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(directory);
        try
        {
            string specification = Path.Combine(directory, "spec.json");
            await File.WriteAllTextAsync(specification, """
                {"components":{"schemas":{"Model":{"type":"object","properties":{"count":{"type":"integer"}}}}},
                 "paths":{"/items":{"get":{"responses":{"200":{"content":{"application/json":{"schema":{"$ref":"#/components/schemas/Model"}}}}}}}}}
                """, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(directory, "Model.cs"), """
                public enum Bad { /* openapi-json-null-sentinel-value-123 */ Null }
                public class Model {
                    public object Count { get; set; }
                    public object GetFieldDeserializers() => new Dictionary<string, Action<IParseNode>> {
                        { "count", n => { Count = n.GetIntValue(); } }
                    };
                }
                public class ItemsRequestBuilder : Base {
                    public ItemsRequestBuilder() : base("{+baseurl}/items") { }
                    public Task<Stream> GetAsync() => null;
                }
                """, cancellationToken);
            var report = await new OpenApiGeneratedCodeDiagnostics().Analyze(specification, directory, cancellationToken);
            report.Passed.Should().BeFalse();
            report.Files.Should().Be(1);
            report.ResponseContracts.Should().Be(1);
            report.Failures.Should().Contain(f => f.StartsWith("OFV001"));
            report.Failures.Should().Contain(f => f.StartsWith("OFV002"));
            report.Failures.Should().Contain(f => f.StartsWith("OFV003"));
            Directory.GetFiles(directory).Length.Should().Be(2);
        }
        finally { Directory.Delete(directory, true); }
    }
}
