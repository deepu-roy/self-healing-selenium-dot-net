using framework.Types;
using OpenQA.Selenium;

namespace framework.Helper;

public partial class TestBase : IDisposable
{
    public IWebDriver Driver;

    public TestBase(bool useMobileResolution = false)
    {
        TestDataManager.Configure();
        Driver = GetDriver(useMobileResolution);
    }

    // Making sure the driver is disposed at the end of each test
    public void Dispose()
    {
        Driver.Quit();
        GC.SuppressFinalize(this);
    }

    private static IWebDriver GetDriver(bool useMobileResolution = false)
    {
        Browser browser = Enum.TryParse(ConfigManager.GetConfiguration("browser"), true, out Browser parsedBrowser)
            ? parsedBrowser
            : throw new ArgumentException("Invalid browser configuration");

        int implicitWaitTimeout = int.Parse(ConfigManager.GetConfiguration("implicitWaitTimeout") ?? "0");
        TimeSpan timeOut = TimeSpan.FromSeconds(implicitWaitTimeout);

        IWebDriver driver;
        bool useHub = bool.TryParse(ConfigManager.GetConfiguration("useHub"), out bool parsedUseHub) && parsedUseHub;
        string hubUrl = ConfigManager.GetConfiguration("hubUrl") ?? "http://localhost:4444/wd/hub";

        if (useHub)
        {
            driver = DriverFactory.CreateInstance(browser, hubUrl);
        }
        else
        {
            driver = DriverFactory.CreateInstance(browser);
        }

        driver.Manage().Timeouts().ImplicitWait = timeOut;

        bool runMobileTests = bool.TryParse(ConfigManager.GetConfiguration("runMobileTests"), out bool runMobile) && runMobile;

        // Browser window size is adjusted only if runMobileTests is true and the test is tagged with Mobile
        // To run only mobile tests, set the runMobileTests to true in the test settings and filter by Mobile tag and run
        if (useMobileResolution && runMobileTests)
            driver.Manage().Window.Size = new System.Drawing.Size(430, 932);
        else
            driver.Manage().Window.Size = new System.Drawing.Size(1920, 1080);

        return driver;
    }
}