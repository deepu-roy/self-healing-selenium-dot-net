using framework.Interfaces;
using framework.Types;
using OpenAI.Chat;
using OpenQA.Selenium;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace framework.Helper;

public class SmartLocatorGenerator : ISmartLocatorGenerator
{
    private static readonly Lazy<SmartLocatorGenerator> _instance = new(() =>
        new SmartLocatorGenerator(OpenApiHelper.Instance));

    private readonly IOpenApiHelper _openApiHelper;

    private static readonly string _cacheFilePath = Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ??
        AppContext.BaseDirectory,
        ConfigManager.GetConfiguration("cachePath") ??
        "locator_cache.json");

    private static readonly ConcurrentDictionary<string, CachedLocatorResult> _locatorCache = new();
    private static readonly object _fileLock = new();

    private readonly string _systemMessage = @"
You are a senior web automation engineer specializing in building robust and maintainable locators for Selenium WebDriver.
Your task is to generate a *more reliable* locator when the original fails — prioritizing *stability and precision* over brevity.
Respond only with valid JSON containing exactly two fields: 'locator' and 'strategy'.
";

    private readonly string _promptTemplate = @"
CONTEXT:
The original locator '{{original_locator}}' is no longer identifying its intended target element.
You are provided with the accessibility tree and HTML source snippet to analyze and generate a reliable alternative.

OBJECTIVE:
Generate a robust alternative locator that uniquely identifies the same element, using only the given HTML and accessibility tree.

---

THINKING PROCESS:
1. **Locate the Intended Element**:
   - Use the accessibility tree to understand what the original locator was supposed to target.
   - Identify its role, label, text, and position.

2. **Diagnose Failure**:
   - Hypothesize why the original locator failed: attribute changes, tag change, dynamic values, or layout shifts.

3. **Analyze the HTML**:
   - Match potential candidate nodes from the accessibility tree to the HTML.
   - Remember: roles in the accessibility tree may map to different HTML tags (e.g., role='button' might be an <input> or <div>).

4. **Generate a New Locator**:
   - Prefer stable, semantically meaningful attributes.
   - Only use tags, classes, and attributes that exist in the provided HTML.
   - Ensure that the locator uniquely and consistently matches only the intended element.

5. **Validate**:
   - Double-check that the locator doesn’t match multiple or wrong elements.

---

LOCATOR PRIORITY (in order):
1. **Stable semantic attributes**: id, name, data-testid, aria-label, role
2. **Stable text-based identifiers**: visible text, aria-labelledby, title
3. **Parent-child relationships** with stable surrounding elements
4. **Static CSS classes** (avoid hashed, dynamic, or BEM variants unless consistent)
5. **Tag + attribute combos** like input[type='submit'] with stable attributes

AVOID:
- Absolute XPath (e.g., //div[2]/span[3])
- Index-based selectors (nth-child, [2]) unless necessary
- Volatile attributes (id=""123_abcd"", timestamp=...)
- Over-specific CSS/XPath that breaks with layout shifts

---

STRATEGY RULES:
- Use ""XPATH"" when:
  - The original was XPath
  - You need complex DOM relationships or text matching
- Use ""CSS"" when:
  - A single or few attribute selectors are sufficient and stable

---

INPUTS:
ORIGINAL LOCATOR:
{{original_locator}}

ACCESSIBILITY TREE (JSON):
{{accessibility_tree_json}}

HTML SNIPPET:
{{page_source_html}}

---

OUTPUT FORMAT:
Respond only with this JSON schema — do not explain or add any other text:

{
  ""locator"": ""<your locator here>"",
  ""strategy"": ""XPATH"" | ""CSS""
}

Generate the most reliable locator now:
";

    private SmartLocatorGenerator(IOpenApiHelper openApiHelper)
    {
        _openApiHelper = openApiHelper;
    }

    public static SmartLocatorGenerator Instance => _instance.Value;

    public static ConcurrentDictionary<string, CachedLocatorResult> LocatorCache => _locatorCache;

    public static void SaveCacheToFile()
    {
        try
        {
            lock (_fileLock)
            {
                var cacheData = _locatorCache.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                var json = JsonSerializer.Serialize(cacheData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                File.WriteAllText(_cacheFilePath, json);
                ConfigManager.GetLogger().Information("Locator cache saved to file: {FilePath} with {Count} entries",
                    _cacheFilePath, cacheData.Count);
            }
        }
        catch (Exception ex)
        {
            ConfigManager.GetLogger().Warning("Failed to save locator cache to file: {Error}", ex.Message);
        }
    }

    public void LoadCacheFromFile()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
            {
                ConfigManager.GetLogger().Information("No existing locator cache file found at: {FilePath}", _cacheFilePath);

                return;
            }

            lock (_fileLock)
            {
                var json = File.ReadAllText(_cacheFilePath);
                var cacheData = JsonSerializer.Deserialize<Dictionary<string, CachedLocatorResult>>(json,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                if (cacheData != null)
                {
                    foreach (var kvp in cacheData)
                    {
                        _locatorCache.TryAdd(kvp.Key, kvp.Value);
                    }

                    ConfigManager.GetLogger().Information("Loaded {Count} cached locators from file: {FilePath}",
                        cacheData.Count, _cacheFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            ConfigManager.GetLogger().Warning("Failed to load locator cache from file: {Error}", ex.Message);
        }
    }

    public void CleanupOldCacheEntries(TimeSpan maxAge)
    {
        var cutoffTime = DateTime.UtcNow - maxAge;
        var keysToRemove = _locatorCache
            .Where(kvp => kvp.Value.Timestamp < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _locatorCache.TryRemove(key, out _);
        }

        if (keysToRemove.Count > 0)
        {
            ConfigManager.GetLogger().Information("Cleaned up {Count} old cache entries", keysToRemove.Count);
        }
    }

    public async Task<By> DoGenerateSmartLocatorWithCacheAsync(IWebDriver driver, By originalLocator)
    {
        if (!bool.TryParse(ConfigManager.GetConfiguration("useSmartLocator"), out var useSmartLocator) || !useSmartLocator)
        {
            return originalLocator;
        }

        var accessibilityTree = driver.GetFullAccessibilityTree();
        if (string.IsNullOrEmpty(accessibilityTree))
        {
            return originalLocator;
        }
        var releventPageSource = driver.GetRelevantHtmlSnippets();
        var sb = new System.Text.StringBuilder();
        foreach (var snippet in releventPageSource)
        {
            sb.AppendLine(snippet);
        }
        var combinedPageSource = sb.ToString();
        // Check if the locator is already cached
        if (_locatorCache.TryGetValue(originalLocator.Criteria, out var cachedResult))
        {
            ConfigManager.GetLogger().Information("Using cached locator for: {OriginalLocator}", originalLocator);
            return cachedResult.ToBy();
        }

        // Not in cache, generate new locator
        try
        {
            var result = await GenerateLocatorInternal(originalLocator.Criteria, accessibilityTree, combinedPageSource);
            _locatorCache.TryAdd(originalLocator.Criteria, result);
            return result.ToBy();
        }
        catch (Exception ex)
        {
            ConfigManager.GetLogger().Warning("Failed to generate locator: {Error}", ex.Message);
            return originalLocator;
        }
    }

    private async Task<CachedLocatorResult> GenerateLocatorInternal(string originalLocator, string accessibilityTreeJson, string pageSourceHtml)
    {
        ChatCompletionOptions options = new()
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "smart_locator",
                jsonSchema: BinaryData.FromBytes("""
                {
                    "type": "object",
                    "properties": {
                        "locator": { "type": "string", "description": "The locator string (XPath or CSS selector)" },
                        "strategy": { "type": "string", "enum": ["XPATH", "CSS"], "description": "The locator strategy to use" }
                    },
                    "required": ["locator", "strategy"],
                    "additionalProperties": false
                }
                """u8.ToArray()),
                jsonSchemaIsStrict: true)
        };

        string prompt = _promptTemplate
            .Replace("{{original_locator}}", originalLocator)
            .Replace("{{accessibility_tree_json}}", accessibilityTreeJson)
            .Replace("{{page_source_html}}", pageSourceHtml);

        JsonDocument response = await _openApiHelper.GetInferenceAsync(_systemMessage, prompt, options);

        var locatorStrategy = response.RootElement.GetProperty("strategy").GetString();
        var locator = response.RootElement.GetProperty("locator").GetString();

        if (string.IsNullOrEmpty(locator) || string.IsNullOrEmpty(locatorStrategy))
        {
            throw new InvalidOperationException("Failed to generate a valid locator or strategy.");
        }

        return new CachedLocatorResult
        {
            OriginalLocator = originalLocator,
            GeneratedLocator = locator,
            Strategy = locatorStrategy.Trim().ToUpperInvariant(),
            Timestamp = DateTime.UtcNow
        };
    }

    public static CacheStatistics GetCacheStatistics()
    {
        return new CacheStatistics
        {
            TotalEntries = LocatorCache.Count,
            OldestEntry = LocatorCache.Values.MinBy(v => v.Timestamp)?.Timestamp,
            NewestEntry = LocatorCache.Values.MaxBy(v => v.Timestamp)?.Timestamp
        };
    }
}