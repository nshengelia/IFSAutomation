using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace IFS.Automation.HelperMethods.Support;

public static class JsonHelpers
{
    private static readonly Regex IndexPattern = new(@"\[(\d+)\]", RegexOptions.Compiled);

    public static JsonNode Parse(string json)
    {
        return JsonNode.Parse(json) ?? throw new InvalidOperationException("Unable to parse JSON content.");
    }

    public static JsonNode? FindNode(JsonNode node, string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "$")
            return node;

        var trimmed = path.Trim();
        trimmed = trimmed.StartsWith("$.", StringComparison.Ordinal) ? trimmed[2..] : trimmed.TrimStart('$');
        var segments = trimmed.Split('.', StringSplitOptions.RemoveEmptyEntries);
        JsonNode? current = node;

        foreach (var segment in segments)
        {
            if (current is null) return null;

            var name = segment;
            var matches = IndexPattern.Matches(segment);
            if (matches.Count > 0)
            {
                var bracketIndex = segment.IndexOf('[', StringComparison.Ordinal);
                name = bracketIndex > 0 ? segment[..bracketIndex] : string.Empty;
            }

            if (!string.IsNullOrEmpty(name))
            {
                current = current switch
                {
                    JsonObject obj when obj.TryGetPropertyValue(name, out var next) => next,
                    _ => null
                };
            }

            foreach (Match match in matches.Cast<Match>())
            {
                if (current is not JsonArray array) return null;
                var index = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                current = index >= 0 && index < array.Count ? array[index] : null;
            }
        }

        return current;
    }

    public static IReadOnlyList<JsonNode?> FindNodes(JsonNode node, string path)
    {
        var target = FindNode(node, path);
        if (target is JsonArray array)
            return array.ToArray();
        return target is null ? Array.Empty<JsonNode?>() : new[] { target };
    }
}
