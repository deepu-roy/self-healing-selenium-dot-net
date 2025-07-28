using framework.Extensions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace framework.Pages;

public class LoginPage(IWebDriver driver)
{
    //Locators
    private readonly int _defaultTimeout = 15;

    private readonly WebDriverWait _wait = new(driver, TimeSpan.FromSeconds(30));
    private readonly IWebDriver _driver = driver;
    private readonly By _loginButton = By.Id("login-button"); // Correct locator example
    //private readonly By _loginButton = By.XPath("//div[contains(text(), 'Submit')]"); //Incorrect locator example, after configuring smart locator generator, even with this locator test would work.
    private readonly By _passwordInput = By.Id("password");
    private readonly By _usernameInput = By.Id("user-name");
    private readonly By _errorMessageContainer = By.CssSelector("div.login-box h3[data-test=error]");

    public void DoLogin(string userName, string password)
    {
        _driver.DoClearAndSendKeys(this._usernameInput, userName, _wait);
        _driver.DoClearAndSendKeys(this._passwordInput, password, _wait);
        _driver.DoClick(this._loginButton);
    }

    public bool IsErrorMessageDisplayed()
    {
        return _driver.DoCheckIfExists(this._errorMessageContainer, _defaultTimeout);
    }

    public bool IsUserOnLoginPage()
    {
        return _driver.DoWaitTillExists(this._usernameInput, this._defaultTimeout);
    }
}