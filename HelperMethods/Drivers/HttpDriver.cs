using System.Net;
using IFS.Automation.HelperMethods.Support;
using Polly;
using RestSharp;

namespace IFS.Automation.HelperMethods.Drivers;

public sealed class HttpDriver
{
    private readonly ApiContext _context;

    public HttpDriver(ApiContext context)
    {
        _context = context;
    }

    public async Task<RestResponse> SendAsync(Method method, string path, int retryCount = 3)
    {
        var baseUrl = _context.BaseUrl
            ?? throw new InvalidOperationException("Base URL must be configured before sending a request.");

        var targetPath = _context.ApplyValues(path);
        var options = new RestClientOptions(baseUrl)
        {
            ThrowOnAnyError = false,
            RemoteCertificateValidationCallback = (_, _, _, _) => true
        };

        if (retryCount > 0)
        {
            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .OrResult<RestResponse>(r =>
                    r.ResponseStatus == ResponseStatus.TimedOut ||
                    r.StatusCode >= HttpStatusCode.InternalServerError)
                .WaitAndRetryAsync(
                    retryCount,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    (_, _, attempt, _) =>
                    {
                        _context.RetryCount = attempt;
                        Console.WriteLine($"Retry attempt {attempt} for {method} {path}");
                    });

            return await retryPolicy.ExecuteAsync(async () =>
            {
                using var client = new RestClient(options);
                return await ExecuteAndSave(client, method, targetPath, baseUrl);
            });
        }

        using var directClient = new RestClient(options);
        return await ExecuteAndSave(directClient, method, targetPath, baseUrl);
    }

    private async Task<RestResponse> ExecuteAndSave(RestClient client, Method method, string path, Uri baseUrl)
    {
        var request = BuildRequest(method, path);
        var response = await client.ExecuteAsync(request);
        _context.LastResponse = response;
        _context.LastRequest = request;
        _context.LastRequestUrl = $"{baseUrl}{path.TrimStart('/')}";
        return response;
    }

    private RestRequest BuildRequest(Method method, string path)
    {
        var request = new RestRequest(path, method);

        foreach (var header in _context.Headers)
            request.AddOrUpdateHeader(header.Key, _context.ApplyValues(header.Value));

        foreach (var query in _context.Query)
            request.AddOrUpdateParameter(query.Key, _context.ApplyValues(query.Value), ParameterType.QueryString);

        if (!string.IsNullOrWhiteSpace(_context.Body))
            request.AddStringBody(_context.ApplyValues(_context.Body!), _context.ContentType ?? "application/json");

        return request;
    }
}
