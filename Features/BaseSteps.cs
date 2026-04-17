using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using IFS.Automation.Config;
using IFS.Automation.HelperMethods.Drivers;
using IFS.Automation.HelperMethods.Support;
using RestSharp;
using TechTalk.SpecFlow;

namespace IFS.Automation.Features;

[Binding]
public sealed class BaseSteps
{
    private readonly ApiContext _context;
    private readonly HttpDriver _driver;
    private JsonNode? _json;

    public BaseSteps(ApiContext context, HttpDriver driver)
    {
        _context = context;
        _driver = driver;
    }

    [Given(@"the API base URL is configured")]
    public void GivenTheApiBaseUrlIsConfigured()
    {
        _context.SetBaseUrl(ApiConfig.BaseUrl);
    }

    [Given(@"a post with ID (\d+) exists")]
    public void GivenAPostExists(int id) { }

    [Given(@"a post with ID (\d+) does not exist")]
    public void GivenAPostDoesNotExist(int id) { }

    [When(@"I send a (\w+) request to (\S+)")]
    public async Task WhenISendARequest(string method, string path)
    {
        await _driver.SendAsync(ParseMethod(method), path);
        _json = null;
    }

    [Then(@"the response status code should be (\d+).*")]
    public void ThenTheResponseStatusCodeShouldBe(int expected)
    {
        var actual = (int)(_context.LastResponse?.StatusCode ?? 0);
        var content = _context.LastContent ?? "(empty)";

        if (actual != expected)
            Assert.Fail($"Expected status {expected} but got {actual}.\nResponse:\n{TryFormatJson(content)}");
    }

    [Then(@"the response should contain a list of posts")]
    public void ThenTheResponseShouldContainAListOfPosts()
    {
        var array = JsonHelpers.FindNodes(GetJson(), "$");
        if (array.Count == 0)
            Assert.Fail("Expected a list of posts but the response was empty.");
    }

    [Then(@"the response should contain exactly (\d+) posts")]
    public void ThenTheResponseShouldContainExactlyPosts(int expected)
    {
        var array = JsonHelpers.FindNodes(GetJson(), "$");
        if (array.Count != expected)
            Assert.Fail($"Expected {expected} posts but got {array.Count}.");
    }

    [Then(@"each post should contain the following fields:")]
    public void ThenEachPostShouldContainTheFollowingFields(Table table)
    {
        var content = _context.LastContent ?? "(empty)";
        foreach (var row in table.Rows)
        {
            var field = row[0];
            var node = JsonHelpers.FindNode(GetJson(), $"$[0].{field}");
            if (node == null || string.IsNullOrWhiteSpace(node.ToString()))
                Assert.Fail($"Field '{field}' was missing or empty in the post.\nResponse:\n{content}");
        }
    }

    [Then(@"the response should contain post with ID (\d+)")]
    public void ThenTheResponseShouldContainPostWithId(int id)
    {
        var node = JsonHelpers.FindNode(GetJson(), "$.id");
        var content = _context.LastContent ?? "(empty)";

        if (node == null)
            Assert.Fail($"Field 'id' was not found in response.\nResponse:\n{content}");

        if (node!.ToString() != id.ToString())
            Assert.Fail($"Expected post ID {id} but got {node}.\nResponse:\n{content}");
    }

    [Then(@"the response body should contain the created post data")]
    public void ThenTheResponseBodyShouldContainTheCreatedPostData()
    {
        var content = _context.LastContent ?? "(empty)";
        foreach (var field in new[] { "id", "userId", "title", "body" })
        {
            var node = JsonHelpers.FindNode(GetJson(), $"$.{field}");
            if (node == null || string.IsNullOrWhiteSpace(node.ToString()))
                Assert.Fail($"Field '{field}' was missing or empty in the created post.\nResponse:\n{content}");
        }
    }

    [Then(@"the response should contain:")]
    public void ThenTheResponseShouldContain(Table table)
    {
        var content = _context.LastContent ?? "(empty)";
        foreach (var row in table.Rows)
        {
            var field = row["field"];
            var expected = row["value"];
            var node = JsonHelpers.FindNode(GetJson(), $"$.{field}");

            if (node == null)
                Assert.Fail($"Field '{field}' was not found in response.\nResponse:\n{content}");

            if (node!.ToString() != expected)
                Assert.Fail($"Field '{field}' expected '{expected}' but got '{node}'.\nResponse:\n{content}");
        }
    }

    [Then(@"the response body should reflect the updated data")]
    public void ThenTheResponseBodyShouldReflectTheUpdatedData()
    {
        var content = _context.LastContent ?? "(empty)";
        foreach (var field in new[] { "id", "title", "body" })
        {
            var node = JsonHelpers.FindNode(GetJson(), $"$.{field}");
            if (node == null || string.IsNullOrWhiteSpace(node.ToString()))
                Assert.Fail($"Field '{field}' was missing or empty in the updated post.\nResponse:\n{content}");
        }
    }

    private JsonNode GetJson()
    {
        if (_json is not null) return _json;
        var content = _context.LastContent;
        content.Should().NotBeNullOrWhiteSpace("response body should not be empty");
        _json = JsonHelpers.Parse(content!);
        return _json;
    }

    private static Method ParseMethod(string method) => method.ToUpperInvariant() switch
    {
        "GET" => Method.Get,
        "POST" => Method.Post,
        "PUT" => Method.Put,
        "PATCH" => Method.Patch,
        "DELETE" => Method.Delete,
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported HTTP method.")
    };

    private static string TryFormatJson(string content)
    {
        try
        {
            var doc = JsonDocument.Parse(content);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch { return content; }
    }
}
