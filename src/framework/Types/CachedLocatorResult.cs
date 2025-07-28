using OpenQA.Selenium;

namespace framework.Types;

public class CachedLocatorResult
{
    public string OriginalLocator { get; set; } = "";
    public string GeneratedLocator { get; set; } = "";
    public string Strategy { get; set; } = "";
    public DateTime Timestamp { get; set; }

    public By ToBy()
    {
        return Strategy switch
        {
            "XPATH" => By.XPath(GeneratedLocator),
            "CSS" => By.CssSelector(GeneratedLocator),
            _ => throw new InvalidOperationException($"Unexpected locator strategy: {Strategy}")
        };
    }
}