using framework.Helper;
using framework.Pages;
using framework.Types;
using OpenQA.Selenium;
using TechTalk.SpecFlow;

namespace tests.Steps;

[Binding]
public class LoginPageSteps(ScenarioContext scenarioContext)
{
    private IWebDriver? _driver;
    private ScenarioContext _scenarioContext = scenarioContext;
    private TestBase? _testBase;

    [Given("the user is on login page")]
    public void GivenTheUserIsOnLoginPage()
    {
        bool useMobileResolution = this._scenarioContext.TryGetValue("mobile", out bool value) && value;
        _testBase = new TestBase(useMobileResolution);
        _driver = _testBase.Driver;
        _scenarioContext.Add("Driver", _testBase.Driver);
        TestDataManager.AddToContext(ref _scenarioContext); //Adding all testdata to the scenario context to make them available during test

        _driver.Navigate().GoToUrl(TestDataManager.GetTestData("BaseUrl"));
        bool userOnLoginPage = new LoginPage(_driver).IsUserOnLoginPage();
        Assert.True(userOnLoginPage, "User is not on login page");
    }

    [When(@"the user logged in with username and password")]
    public void WhenTheUserLoggedInValidWithUserNamePassword()
    {
        if (_testBase == null || TestDataManager.TestData == null)
            return;
        try
        {
            if (_driver == null)
            { return; }
            new LoginPage(_driver)
           .DoLogin(TestDataManager.GetTestData("UserName"), TestDataManager.GetTestData("Password"));
        }
        catch (Exception e)
        {
            ExceptionHandler.Handle(e);
        }
    }

    [When(@"the user logged in with ""([^""]*)"" and ""([^""]*)""")]
    public void WhenTheUserLoggedInWithAnd(string username, string password)
    {
        if (_testBase == null || TestDataManager.TestData == null)
            return;
        try
        {
            if (_driver == null)
            { return; }
            new LoginPage(_driver)
           .DoLogin(username, password);
        }
        catch (Exception e)
        {
            ExceptionHandler.Handle(e);
        }
    }

    [Then(@"the user is shown error message")]
    public void ThenTheUserIsShownErrorMessage()
    {
        if (_driver == null)
        { return; }
        bool errorMessageDisplayed = new LoginPage(_driver).IsErrorMessageDisplayed();
        Assert.True(errorMessageDisplayed, "Error Message is not displayed for invalid credentials");
    }
}