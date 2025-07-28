namespace framework.Helper;

public static class ExceptionHandler
{
    public static void Handle(Exception e)
    {
        if (e is OpenQA.Selenium.ElementNotInteractableException)
        {
            Console.WriteLine("Test Failed. ElementNotInteractableException occured");
            throw e;
        }
        else if (e is OpenQA.Selenium.NoSuchFrameException)
        {
            Console.WriteLine("Test Failed. NoSuchFrameException occured");
            throw e;
        }
        else if (e is OpenQA.Selenium.NoAlertPresentException)
        {
            Console.WriteLine("Test Failed. NoAlertPresentException occured");
            throw e;
        }
        else if (e is OpenQA.Selenium.NoSuchWindowException)
        {
            Console.WriteLine("Test Failed. NoSuchWindowException occured");
            throw e;
        }
        else if (e is OpenQA.Selenium.StaleElementReferenceException)
        {
            Console.WriteLine("Test Failed. StaleElementReferenceException occured");
            throw e;
        }
        else if (e is OpenQA.Selenium.WebDriverException)
        {
            Console.WriteLine("Test Failed. WebDriverException occured");
            throw e;
        }
        else
        {
            Console.WriteLine("Test Failed. Exception occured");
            throw e;
        }
    }
}