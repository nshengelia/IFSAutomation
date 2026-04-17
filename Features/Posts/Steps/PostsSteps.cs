using System.Text.Json;
using System.Text.Json.Nodes;
using IFS.Automation.HelperMethods.Support;
using TechTalk.SpecFlow;

namespace IFS.Automation.Features.Posts.Steps;

[Binding]
public sealed class PostsSteps
{
    private readonly ApiContext _context;

    public PostsSteps(ApiContext context)
    {
        _context = context;
    }

    [Given(@"I have a valid post payload")]
    public void GivenIHaveAValidPostPayload()
    {
        var payload = new
        {
            userId = 1,
            title = "Test Title",
            body = "Test Body"
        };
        _context.SetBody(JsonSerializer.Serialize(payload));
    }

    [Given(@"I have a post payload with:")]
    public void GivenIHaveAPostPayloadWith(Table table)
    {
        var dict = table.Rows.ToDictionary(r => r["field"], r => r["value"]);
        var payload = new JsonObject();
        foreach (var pair in dict)
        {
            if (int.TryParse(pair.Value, out var number))
                payload[pair.Key] = number;
            else
                payload[pair.Key] = pair.Value;
        }
        _context.SetBody(payload.ToJsonString());
    }

    [Given(@"I have updated post data")]
    public void GivenIHaveUpdatedPostData()
    {
        var payload = new
        {
            id = 1,
            userId = 1,
            title = "Updated Title",
            body = "Updated Body"
        };
        _context.SetBody(JsonSerializer.Serialize(payload));
    }
}
