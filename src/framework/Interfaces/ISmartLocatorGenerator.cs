using OpenQA.Selenium;

namespace framework.Interfaces;

public interface ISmartLocatorGenerator
{
    Task<By> DoGenerateSmartLocatorWithCacheAsync(IWebDriver driver, By originalLocator);

    void LoadCacheFromFile();

    void CleanupOldCacheEntries(TimeSpan maxAge);
}