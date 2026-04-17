using RestSharp;

namespace IFS.Automation.HelperMethods.Support;

public sealed class ApiContext
{
    public Uri? BaseUrl { get; private set; }
    public Dictionary<string, string> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Query { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string? Body { get; private set; }
    public string? ContentType { get; private set; }
    public string? LastRequestBody { get; set; }
    public string? LastRequestUrl { get; set; }
    public RestRequest? LastRequest { get; set; }
    public RestResponse? LastResponse { get; set; }
    public string? LastContent => LastResponse?.Content;
    public int RetryCount { get; set; }

    private Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void SetBaseUrl(string baseUrl) => BaseUrl = new Uri(baseUrl, UriKind.Absolute);

    public void SetBody(string body, string? contentType = null)
    {
        Body = body;
        LastRequestBody = body;
        ContentType = contentType ?? "application/json";
    }

    public void ClearRequest()
    {
        Headers.Clear();
        Query.Clear();
        Body = null;
        ContentType = null;
        RetryCount = 0;
    }

    public void SaveValue(string key, string value) => Values[key] = value;

    public string GetValue(string key) =>
        Values.TryGetValue(key, out var value)
            ? value
            : throw new InvalidOperationException($"Value '{key}' was not found in the scenario context.");

    public string ApplyValues(string template)
    {
        var result = template;
        foreach (var pair in Values)
            result = result.Replace($"{{{pair.Key}}}", pair.Value, StringComparison.OrdinalIgnoreCase);
        return result;
    }
}
