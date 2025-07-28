using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Events;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace framework.Helper;

public static class ConfigManager
{
    private static readonly ILogger _logger;
    private static readonly Lazy<IImmutableDictionary<string, string>> _configs;
    private static readonly ISet<string> _configNames;

    static ConfigManager()
    {
        _configs = new(LoadConfigs);
        _configNames = new HashSet<string>
        {
            "implicitWaitTimeout", "browser","hubUrl", "useHub","runMobileTests",
            "environment", "takeScreenshot", "takeScreenshotOnlyForFailedStep",
            "logFilePath","logLevel","failForConsoleError",
            "useSmartLocator","runWithSmartLocator","openApiModel","openApiKey","cachePath"
        };
        _logger = InitializeLogger();
    }

    [DebuggerHidden]
    public static ILogger GetLogger()
    {
        return _logger;
    }

    private static ImmutableDictionary<string, string> LoadConfigs()
    {
        var configBuilder = ImmutableDictionary.CreateBuilder<string, string>();
        var configFilePath = "./testsettings.json";

        if (!File.Exists(configFilePath))
        {
            throw new Exception($"Configuration file not found: {configFilePath}");
        }

        var configData = File.ReadAllText(configFilePath);
        var jsonConfig = JObject.Parse(configData);

        foreach (var configName in _configNames)
        {
            var envVarNameCamelCase = ToSnakeCase(configName).ToUpper();
            var envVarNameCapital = configName.ToUpper();

            var configValue = Environment.GetEnvironmentVariable(envVarNameCamelCase) ??
                Environment.GetEnvironmentVariable(envVarNameCapital)
                               ?? jsonConfig.Value<string>(configName);

            if (configValue != null)
            {
                configBuilder[configName] = configValue;
            }
            else
            {
                throw new Exception($"Missing configuration: {configName} (Env: {envVarNameCamelCase})");
            }
        }

        return configBuilder.ToImmutable();
    }

    public static string? GetConfiguration(string configurationName)
    {
        return _configs.Value.TryGetValue(configurationName, out var configurationValue) ? configurationValue : null;
    }

    private static Serilog.Core.Logger InitializeLogger()
    {
        var logLevel = (GetConfiguration("logLevel")?.ToLower()) switch
        {
            "debug" => LogEventLevel.Debug,
            "info" => LogEventLevel.Information,
            "warning" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            _ => LogEventLevel.Error,
        };

        // Set the minimum log level globally in your configuration.
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)  // Set the minimum log level here
            .WriteTo.Console(logLevel)
            .WriteTo.File(GetConfiguration("logFilePath") ?? string.Empty, logLevel);

        var logger = loggerConfiguration.CreateLogger();
        Log.Logger = logger;
        return logger;
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var stringBuilder = new StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsUpper(c) && i > 0)
            {
                stringBuilder.Append('_');
            }
            stringBuilder.Append(char.ToUpperInvariant(c));
        }
        return stringBuilder.ToString();
    }
}