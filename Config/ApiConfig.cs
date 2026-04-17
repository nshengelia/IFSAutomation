namespace IFS.Automation.Config;

public static class ApiConfig
{
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("API_BASEURL")
        ?? ConfigurationHelper.Configuration["ApiConfig:BaseUrl"]
        ?? throw new Exception("ApiConfig:BaseUrl is not configured");
}
