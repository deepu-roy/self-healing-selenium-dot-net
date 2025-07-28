using framework.Extensions;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace framework.Pages;

public class InventoryPage(IWebDriver driver)
{
    // Locators
    private readonly int _defaultTimeout = 15;

    private readonly WebDriverWait _wait = new(driver, TimeSpan.FromSeconds(15));
    private readonly By _inventoryHeader = By.CssSelector("#inventory_filter_container div.product_label");
    private readonly By _productImages = By.CssSelector("div.inventory_item_img img");
    private readonly IWebDriver _driver = driver;

    public bool IsUserOnInventoryPage()
    {
        return _driver.DoCheckIfExists(this._inventoryHeader, this._defaultTimeout);
    }

    public bool AreProductImagesUnique()
    {
        var allImageElements = _driver.FindElements(this._productImages);
        string imageSource = allImageElements.ElementAt(0).GetAttribute("src") ?? string.Empty;

        foreach (var imageElement in allImageElements)
        {
            if (imageSource != imageElement.GetAttribute("src")) return false;
        }
        return true;
    }

    public bool AreProductImagesInvalid()
    {
        var allImageElements = _driver.FindElements(this._productImages);
        foreach (var imageElement in allImageElements)
        {
            var imageSource = imageElement.GetAttribute("src") ?? string.Empty;
            if (imageSource.Contains("WithGarbageOnItToBreakTheUrl")) return true;
        }
        return false;
    }
}