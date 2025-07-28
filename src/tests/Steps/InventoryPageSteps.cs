using framework.Pages;
using OpenQA.Selenium;

namespace tests.Steps;

[Binding]
public class InventoryPageSteps
{
    private readonly IWebDriver _driver;
    private readonly ScenarioContext _scenarioContext;

    public InventoryPageSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _scenarioContext.TryGetValue("Driver", out _driver);
    }

    [Then(@"the user is navigated to Inventory Page")]
    public void ThenUserIsOnTheHomePage()
    {
        bool isUserOnInventoryPage = new InventoryPage(this._driver).IsUserOnInventoryPage();
        Assert.True(isUserOnInventoryPage, "User is not on inventory page");
    }

    [Then(@"the product images are all the same")]
    public void ThenTheProductImagesAreAllTheSame()
    {
        bool isProductImagesTheSame = new InventoryPage(this._driver).AreProductImagesUnique();
        Assert.True(isProductImagesTheSame, "Problem user is shown different images");
    }

    
    [Then(@"the product images are all invalid")]
    public void ThenTheProductImagesAreAllInvalid()
    {
        bool invalidProductImages = new InventoryPage(this._driver).AreProductImagesInvalid();
        Assert.True(invalidProductImages, "Problem user is shown valid images");
    }
}