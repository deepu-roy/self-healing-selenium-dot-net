using framework.Helper;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace framework.Extensions;

public static partial class WebDriverExtensions
{
    public static void DoClearAndSendKeys(this IWebDriver driver, By element, string value, WebDriverWait? wait = null)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var webElement = driver.DoGetElementWithWait(element, 5, $"Element with locator {element.Criteria} was not found on page {driver.Url}.");
        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
        js.ExecuteScript("arguments[0].value = '';", webElement);
        webElement?.SendKeys(value);
    }

    public static void DoClick(this IWebDriver driver, By element, WebDriverWait? wait = null)
    {
        if (wait == null)
        {
            var time = TimeSpan.FromSeconds(double.Parse(ConfigManager.GetConfiguration("implicitWaitTimeout") ?? "5"));
            wait = new WebDriverWait(driver, time);
        }
        element = driver.DoGetLocatorWithWait(element, 5, $"Element with locator {element.Criteria} was not found on page {driver.Url}.");
        new Actions(driver).MoveToElement(driver.FindElement(element)).Perform();
        driver.FindElement(element).Click();
    }


    public static bool DoWaitTillExists(this IWebDriver driver, By element, double? seconds = 5, string? customMessage = null)
    {
        var isFound = driver.DoCheckIfExists(element, seconds);
        if (isFound) return true;
        throw new Exception($"Element with locator {element.Criteria} was not found within {seconds} seconds on page {driver.Url}.\n{customMessage}");
    }

    public static bool DoCheckIfExists(this IWebDriver driver, By element, double? seconds = 5)
    {
        seconds ??= 5;
        var time = TimeSpan.FromSeconds((double)seconds);
        var wait = new WebDriverWait(driver, time);
        try
        {
            wait.Until(ExpectedConditions.ElementExists(element));
            return true;
        }
        catch
        {
            return false;
        }
    }
    public static IWebElement DoGetElementWithWait(this IWebDriver driver, By locator, double waitUntil = 2, string? errorMessage = null)
    {
        var targetLocator = driver.DoGetLocatorWithWait(locator, waitUntil, errorMessage);
        return driver.FindElement(targetLocator);
    }

    public static By DoGetLocatorWithWait(this IWebDriver driver, By locator, double waitUntil = 2, string? errorMessage = null)
    {
        if (driver.DoCheckIfExists(locator, waitUntil))
            return locator;

        var smartLocator = driver.DoResolveSmartLocator(locator);
        if (smartLocator != locator && driver.DoCheckIfExists(smartLocator, waitUntil))
            return smartLocator;

        Assert.Fail(errorMessage ?? ErrorMessages.GetFormattedMessage(ErrorMessages.LocatorNotFound, locator, waitUntil));
        return locator;
    }

    public static By DoResolveSmartLocator(this IWebDriver driver, By locator)
    {
        if (driver.DoCheckIfExists(locator, 0.2))
        {
            return locator;
        }

        var smartLocator = SmartLocatorGenerator.Instance.DoGenerateSmartLocatorWithCacheAsync(driver, locator).GetAwaiter().GetResult();
        if (string.IsNullOrEmpty(smartLocator.Criteria) || !driver.DoCheckIfExists(smartLocator, 0.2))
        {
            ConfigManager.GetLogger().Information($"Smart Locator generated is not valid");
            return locator;
        }

        ConfigManager.GetLogger().Information($"Valid smart Locator generated: {smartLocator.Criteria}");
        if (bool.TryParse(ConfigManager.GetConfiguration("runWithSmartLocator"), out var useSmartLocator) && useSmartLocator)
        {
            return smartLocator;
        }

        Assert.Fail($"Locator has been changed from \nOld: {locator.Criteria}\nNew: {smartLocator.Criteria}\nPlease review the same and update the test or create a bug");
        return locator;
    }
}