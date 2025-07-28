using AventStack.ExtentReports;
using AventStack.ExtentReports.Gherkin.Model;
using AventStack.ExtentReports.Model;

using AventStack.ExtentReports.Reporter;

using framework.Helper;
using OpenQA.Selenium;
using TechTalk.SpecFlow.Infrastructure;

namespace tests.Hooks;

[Binding]
public class TestHooks
{
    private static ExtentReports? extent;
    private static FeatureContext? _featureContext;
    private static ExtentTest? _featureName;
    private ExtentTest? _currentScenarioName;
    private readonly ScenarioContext _scenarioContext;
    private readonly ISpecFlowOutputHelper _specFlowOutputHelper;
    public static ExtentReports? Extent { get => extent; set => extent = value; }

    static TestHooks()
    {
        TestHooks.extent = new ExtentReports();
    }

    public TestHooks(FeatureContext _featureContext, ScenarioContext _scenarioContext, ISpecFlowOutputHelper _specFlowOutputHelper)
    {
        this._scenarioContext = _scenarioContext;
        this._specFlowOutputHelper = _specFlowOutputHelper;
        TestHooks._featureContext = _featureContext;
    }

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        var reportPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        var reportName = DateTime.Now.ToString("_MMddyyyy_hhmmtt") + ".html";
        var reportFullName = $"{reportPath}{Path.DirectorySeparatorChar}{reportName}";
        var htmlreporter = new ExtentSparkReporter(reportFullName);
        var _locatorGenerator = SmartLocatorGenerator.Instance;
        _locatorGenerator.LoadCacheFromFile();
        _locatorGenerator.CleanupOldCacheEntries(TimeSpan.FromDays(3));

        var stats = SmartLocatorGenerator.GetCacheStatistics();
        ConfigManager.GetLogger().Information("Test run started with {TotalEntries} cached locators", stats.TotalEntries);
        Extent?.AttachReporter(htmlreporter);
    }

    [AfterTestRun]
    public static void ExtentClose()
    {
        Extent?.Flush();
        try
        {
            if (SmartLocatorGenerator.LocatorCache != null)
            {
                SmartLocatorGenerator.SaveCacheToFile();
                var stats = SmartLocatorGenerator.GetCacheStatistics();
                ConfigManager.GetLogger().Information("Test run completed. Cache saved with {TotalEntries} entries", stats.TotalEntries);
            }
        }
        catch (Exception ex)
        {
            ConfigManager.GetLogger().Error("Failed to save locator cache: {Error}", ex.Message);
        }

        var (TotalRequests, TotalTokens, TotalDuration, AverageTokensPerRequest, AverageDurationPerRequest) = OpenApiHelper.TelemetryStats.GetStats();
        ConfigManager.GetLogger().Information($"Open Api Telemetry:\n\tTotal Duration: {TotalDuration}\n\tTotal Requests:{TotalRequests}\n\tTotal Tokens:{TotalTokens}\n\tAverage Tokens:{AverageTokensPerRequest}\n\tAverage Duration:{AverageDurationPerRequest}");
        ConfigManager.GetLogger().Information($"Logs are flushed");
        Serilog.Log.CloseAndFlush();
        Console.Out.Flush();
        Console.Error.Flush();
    }

    [AfterScenario]
    public void AfterScenarioCleanup()
    {
        IWebDriver? _driver = null;
        _scenarioContext?.TryGetValue("Driver", out _driver);
        Extent?.Flush();
        _driver?.Dispose();
    }

    [AfterStep]
    public void AfterStep()
    {
        var stepType = _scenarioContext.StepContext.StepInfo.StepDefinitionType.ToString();

        IWebDriver? _driver = null;
        _scenarioContext?.TryGetValue("Driver", out _driver);
        var takeScreenshot = bool.Parse(ConfigManager.GetConfiguration("takeScreenshot") ?? "false");
        var takeScreenshotOnlyForFailedStep = Boolean.Parse(ConfigManager.GetConfiguration("takeScreenshotOnlyForFailedStep") ?? "false");

        Media? mediaEntity = null;
        if ((takeScreenshot && !takeScreenshotOnlyForFailedStep) || (takeScreenshot && takeScreenshotOnlyForFailedStep && _scenarioContext?.TestError != null))
        {
            if (!Directory.Exists("Screenshots"))
            {
                Directory.CreateDirectory("Screenshots");
            }
            var fileName = Path.ChangeExtension($"Screenshots{Path.DirectorySeparatorChar}{Path.GetRandomFileName()}", "png");
            var screenShot = (_driver as ITakesScreenshot)?.GetScreenshot();

            screenShot?.SaveAsFile(fileName);
            _specFlowOutputHelper.AddAttachment(fileName);
            if (screenShot != null)
            {
                var screenShotEncoded = screenShot?.AsBase64EncodedString;
                mediaEntity = MediaEntityBuilder.CreateScreenCaptureFromBase64String(screenShotEncoded, _scenarioContext?.ScenarioInfo.Title.Trim()).Build();
            }
        }

        if (_scenarioContext == null)
            return;

        if (_scenarioContext.TestError == null)
        {
            switch (stepType)
            {
                case "Given":
                    _currentScenarioName?.CreateNode<Given>(_scenarioContext.StepContext.StepInfo.Text).Pass("Passed", mediaEntity);
                    break;

                case "When":
                    _currentScenarioName?.CreateNode<When>(_scenarioContext.StepContext.StepInfo.Text).Pass("Passed", mediaEntity);
                    break;

                case "And":
                    _currentScenarioName?.CreateNode<And>(_scenarioContext.StepContext.StepInfo.Text).Pass("Passed", mediaEntity);
                    break;

                case "Then":
                    _currentScenarioName?.CreateNode<Then>(_scenarioContext.StepContext.StepInfo.Text).Pass("Passed", mediaEntity);
                    break;
            }
        }
        if (_scenarioContext.TestError != null)
        {
            switch (stepType)
            {
                case "Given":
                    _currentScenarioName?.CreateNode<Given>(_scenarioContext?.StepContext.StepInfo.Text).Fail(_scenarioContext?.TestError.Message, mediaEntity);
                    break;

                case "When":
                    _currentScenarioName?.CreateNode<When>(_scenarioContext?.StepContext.StepInfo.Text).Fail(_scenarioContext?.TestError.Message, mediaEntity);
                    break;

                case "And":
                    _currentScenarioName?.CreateNode<And>(_scenarioContext?.StepContext.StepInfo.Text).Fail(_scenarioContext?.TestError.Message, mediaEntity);
                    break;

                case "Then":
                    _currentScenarioName?.CreateNode<Then>(_scenarioContext?.StepContext.StepInfo.Text).Fail(_scenarioContext?.TestError.Message, mediaEntity);
                    break;
            }
        }
    }

    [BeforeScenario]
    public void BeforeScenario()
    {
        if (_featureContext != null && Extent != null)
            TestHooks._featureName = Extent.CreateTest<Feature>(_featureContext.FeatureInfo.Title);
        this._currentScenarioName = TestHooks._featureName?.CreateNode<Scenario>(_scenarioContext.ScenarioInfo.Title);

        // Check if the current sceanario should be run on a mobile browser. If the scenario is tagged with @headedBrowser then this test is run on headed browser.
        if (_scenarioContext != null && _scenarioContext.ScenarioInfo.Tags.ToList().Exists(x => x.ToLower().Equals("mobile")))
        {
            ConfigManager.GetLogger().Information($"Test:'{_scenarioContext.ScenarioInfo.Title}' to be run on mobile browser resolution");
            _scenarioContext.Add("mobile", true);
        }

        bool runMobileTests = bool.TryParse(ConfigManager.GetConfiguration("runMobileTests"), out bool runMobile) && runMobile;
        if (!runMobileTests && _scenarioContext != null && _scenarioContext.ScenarioInfo.Tags.ToList().Exists(x => x.Equals("NoDesktop", StringComparison.InvariantCultureIgnoreCase)))
            throw new SkipException($"Scenario '{_scenarioContext.ScenarioInfo.Title}' is skipped based on the '@NoDesktop' tag.");
    }
}