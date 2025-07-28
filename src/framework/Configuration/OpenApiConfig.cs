using framework.Helper;

namespace framework.Configuration;

public class OpenApiConfig
{
    public string ApiKey { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;

    public static OpenApiConfig Load()
    {
        return new OpenApiConfig
        {
            ApiKey = ConfigManager.GetConfiguration("openApiKey")
                ?? throw new InvalidOperationException("API key not found in configuration"),
            Model = ConfigManager.GetConfiguration("openApiModel")
                ?? throw new InvalidOperationException("Model not found in configuration")
        };
    }
}