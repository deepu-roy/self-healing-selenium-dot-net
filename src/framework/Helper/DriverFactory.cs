using framework.Types;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

namespace framework.Helper;

public static class DriverFactory
{
    public static IWebDriver CreateInstance(Browser browserType)
    {
        IWebDriver? driver;
        switch (browserType)
        {
            case Browser.Chrome:
                var chromeOptions = new ChromeOptions();
                chromeOptions.AddArguments("--lang=en_US");
                chromeOptions.AddArguments("window-size=1920,1080");
                chromeOptions.AddUserProfilePreference("safebrowsing.enabled", "false"); //safe browsing disabled as other wise xml files can not be downloaded
                new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);
                driver = new ChromeDriver(chromeOptions);
                break;

            case Browser.Edge:
                var edgeOptions = new EdgeOptions();
                new DriverManager().SetUpDriver(new EdgeConfig(), VersionResolveStrategy.Latest);
                driver = new EdgeDriver(edgeOptions);
                break;

            case Browser.Firefox:
                new DriverManager().SetUpDriver(new FirefoxConfig(), VersionResolveStrategy.Latest);
                driver = new FirefoxDriver();
                break;

            case Browser.Headless:
                var chromeHeadlessOptions = new ChromeOptions();
                chromeHeadlessOptions.AddArguments("--no-sandbox");
                chromeHeadlessOptions.AddArguments("--headless=new");
                chromeHeadlessOptions.AddArguments("--lang=en_US");
                chromeHeadlessOptions.AddArguments("--disable-gpu");
                chromeHeadlessOptions.AddArguments("--disable-extensions");
                chromeHeadlessOptions.AddArguments("--disable-dev-shm-usage");
                chromeHeadlessOptions.AddUserProfilePreference("safebrowsing.enabled", "false"); //safe browsing disabled as other wise xml files can not be downloaded
                new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.MatchingBrowser);
                driver = new ChromeDriver(chromeHeadlessOptions);
                break;

            default:
                throw new Exception($"Browser is not configured correctly");
        }
        return driver;
    }

    public static IWebDriver CreateInstance(Browser? browserType, string? hubUrl)
    {
        hubUrl ??= @"http://localhost:4444/wd/hub";
        IWebDriver? driver;
        switch (browserType)
        {
            default:
            case Browser.Chrome:
                ChromeOptions chromeOptions = new();
                driver = GetWebDriver(hubUrl, chromeOptions.ToCapabilities());
                break;

            case Browser.Edge:
                EdgeOptions options = new();
                driver = GetWebDriver(hubUrl, options.ToCapabilities());
                break;

            case Browser.Firefox:
                FirefoxOptions firefoxOptions = new();
                driver = GetWebDriver(hubUrl, firefoxOptions.ToCapabilities());
                break;
        }

        return driver;
    }

    private static RemoteWebDriver GetWebDriver(string hubUrl, ICapabilities capabilities)
    {
        TimeSpan timeSpan = new(0, 3, 0);
        return new RemoteWebDriver(new Uri(hubUrl), capabilities, timeSpan);
    }
}