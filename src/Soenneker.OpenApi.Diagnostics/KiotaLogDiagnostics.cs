using System.Text.RegularExpressions;
using Soenneker.OpenApi.Diagnostics.Abstract;
using Soenneker.OpenApi.Diagnostics.Models;

namespace Soenneker.OpenApi.Diagnostics;

public sealed class KiotaLogDiagnostics : IKiotaLogDiagnostics
{
    private static readonly Regex Header = new(@"^(warn|fail|error|crit):\s+[^\[\r\n]+\[([^\]]+)\]\s*(.*)$", RegexOptions.Compiled);
    private static readonly Regex Prefix = new(@"^\uFEFF?(?:\[[^\]\r\n]+\][ \t]?)+", RegexOptions.Compiled);
    private static readonly Regex Level = new(@"^(trace|debug|info|warn|fail|error|crit):", RegexOptions.Compiled);
    private static readonly (string Fragment, string Category)[] Categories =
    [
        ("polymorphic type but does not define a discriminator", "missing-discriminator"),
        ("is not inherited from", "discriminator-inheritance"),
        ("is not a valid model and points to a union type", "discriminator-union-target"),
        ("The format ", "unsupported-format"),
        ("url form encoded", "complex-form"),
        ("request body which is not an object type", "complex-form"),
        ("GET operation with a body", "get-body"),
        ("Multiple servers entries", "multiple-servers"),
        ("No server url found", "missing-server"),
        ("A servers entry (v3)", "missing-server"),
        ("Ignoring duplicate parameter", "duplicate-parameter"),
        ("Could not create error type", "error-type")
    ];

    public KiotaDiagnosticReport Analyze(string log)
    {
        ArgumentNullException.ThrowIfNull(log);
        var diagnostics = new List<KiotaDiagnostic>();
        var message = new List<string>();
        string? severity = null, code = null;
        void Append()
        {
            if (severity == null) return;
            string text = string.Join(" ", message);
            string category = Categories.FirstOrDefault(c => text.Contains(c.Fragment, StringComparison.Ordinal)).Category ?? "other";
            diagnostics.Add(new KiotaDiagnostic { Severity = severity, Code = code ?? "", Category = category, Message = text });
            message.Clear();
        }

        using var reader = new StringReader(log);
        while (reader.ReadLine() is { } raw)
        {
            string line = Prefix.Replace(raw.TrimStart('\uFEFF'), "");
            Match header = Header.Match(line);
            if (header.Success)
            {
                Append();
                severity = header.Groups[1].Value;
                code = header.Groups[2].Value;
                if (!string.IsNullOrWhiteSpace(header.Groups[3].Value)) message.Add(header.Groups[3].Value.Trim());
            }
            else if (severity != null && !string.IsNullOrWhiteSpace(line) &&
                     (char.IsWhiteSpace(line[0]) || Prefix.IsMatch(raw) && !Level.IsMatch(line)))
                message.Add(line.Trim());
            else
            {
                Append();
                severity = code = null;
            }
        }
        Append();
        return new KiotaDiagnosticReport
        {
            Diagnostics = diagnostics,
            Categories = new SortedDictionary<string, int>(diagnostics.GroupBy(d => d.Category)
                .ToDictionary(g => g.Key, g => g.Count()), StringComparer.Ordinal)
        };
    }
}
