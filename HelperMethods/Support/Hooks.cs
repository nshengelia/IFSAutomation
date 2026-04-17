using System.Text;
using BoDi;
using IFS.Automation.Config;
using IFS.Automation.HelperMethods.Drivers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Events;
using TechTalk.SpecFlow;

namespace IFS.Automation.HelperMethods.Support;

[Binding]
public sealed class Hooks
{
    private readonly IObjectContainer _container;
    private readonly ScenarioContext _scenarioContext;
    private string? _failedStepText;
    private DateTime _scenarioStartTime;
    private readonly List<string> _executedSteps = new();
    private int _skippedStepsCount;

    public Hooks(IObjectContainer container, ScenarioContext scenarioContext)
    {
        _container = container;
        _scenarioContext = scenarioContext;
    }

    [BeforeTestRun]
    public static void InitRun()
    {
        var logDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                restrictedToMinimumLevel: LogEventLevel.Information,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}")
            .WriteTo.Async(a => a.File(
                Path.Combine(logDirectory, "test-report-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                shared: true))
            .CreateLogger();

        Log.Information("TEST RUN STARTED");
    }

    [BeforeScenario(Order = 0)]
    public void InitScenario()
    {
        _scenarioStartTime = DateTime.UtcNow;
        _failedStepText = null;
        _executedSteps.Clear();
        _skippedStepsCount = 0;

        var context = new ApiContext();
        context.SetBaseUrl(ApiConfig.BaseUrl);
        _container.RegisterInstanceAs(context);
        _container.RegisterInstanceAs(new HttpDriver(context));
    }

    [AfterStep]
    public void AfterStep()
    {
        var stepInfo = _scenarioContext.StepContext.StepInfo;
        var status = _scenarioContext.StepContext.Status;
        var stepText = $"{stepInfo.StepDefinitionType} {stepInfo.Text}";

        if (_failedStepText != null && status != ScenarioExecutionStatus.TestError)
        {
            _skippedStepsCount++;
            return;
        }

        switch (status)
        {
            case ScenarioExecutionStatus.OK:
                _executedSteps.Add(stepText);
                break;
            case ScenarioExecutionStatus.TestError:
                if (_failedStepText == null)
                {
                    _failedStepText = stepInfo.Text;
                    _executedSteps.Add($"{stepText} -> FAILED");
                }
                else
                {
                    _skippedStepsCount++;
                }
                break;
            default:
                _skippedStepsCount++;
                break;
        }
    }

    [AfterScenario]
    public void CleanupScenario()
    {
        var hasFailed = _scenarioContext.TestError != null;
        var duration = DateTime.UtcNow - _scenarioStartTime;
        var scenario = _scenarioContext.ScenarioInfo.Title;
        var apiContext = _container.IsRegistered<ApiContext>() ? _container.Resolve<ApiContext>() : null;

        var report = new StringBuilder();
        report.AppendLine();
        report.AppendLine("========== Test Report ==========");
        report.AppendLine($"Scenario:  {scenario}");
        report.AppendLine($"Status:    {(hasFailed ? "FAILED" : "PASSED")}");
        report.AppendLine($"Duration:  {duration.TotalSeconds:F2}s");
        var retries = apiContext?.RetryCount ?? 0;
        report.AppendLine($"Retries:   {(retries == 0 ? "none" : $"{retries} retry attempt(s) before passing")}");
        report.AppendLine();
        report.AppendLine($"URL: {apiContext?.LastRequestUrl ?? "N/A"}");
        report.AppendLine();

        if (!string.IsNullOrEmpty(apiContext?.LastRequestBody))
        {
            report.AppendLine("Request Body:");
            report.AppendLine(BeautifyJson(apiContext.LastRequestBody));
        }
        else
        {
            report.AppendLine("Request Body: (none)");
        }

        report.AppendLine();

        if (apiContext?.LastResponse != null)
        {
            report.AppendLine($"Response ({(int)apiContext.LastResponse.StatusCode}):");
            report.AppendLine(BeautifyJson(apiContext.LastResponse.Content));
        }
        else
        {
            report.AppendLine("Response: (none)");
        }

        report.AppendLine();
        report.AppendLine("Steps:");
        foreach (var step in _executedSteps)
            report.AppendLine($"  {step}");
        if (_skippedStepsCount > 0)
            report.AppendLine($"  ... {_skippedStepsCount} step(s) skipped");

        if (hasFailed)
        {
            var errorMessage = _scenarioContext.TestError?.Message?.Split('\n')[0].Trim();
            report.AppendLine();
            report.AppendLine($"Failed Step: {_failedStepText ?? "Unknown"}");
            report.AppendLine($"Error:       {errorMessage}");
        }

        report.AppendLine("=================================");

        if (hasFailed)
            Log.Error(report.ToString());
        else
            Log.Information(report.ToString());

        if (_container.IsRegistered<ApiContext>())
        {
            var ctx = _container.Resolve<ApiContext>();
            ctx.ClearRequest();
            ctx.LastResponse = null;
            ctx.LastRequest = null;
            ctx.LastRequestUrl = null;
        }
    }

    [AfterTestRun]
    public static void AfterTestRun()
    {
        Log.Information("TEST RUN COMPLETED");
        Log.CloseAndFlush();
    }

    private static string BeautifyJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "(empty)";
        try { return JToken.Parse(json).ToString(Formatting.Indented); }
        catch { return json; }
    }
}
