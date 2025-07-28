namespace framework.Helper;

public static class ErrorMessages
{
    public const string PageNotLoaded = "Page/Module '{0}' did not appear in '{1}' seconds. Please check if the module name is correct OR if the page has not loaded successfully OR if the locator '{2}' has changed.";
    public const string LocatorNotFound = "Locator '{0}' did not appear in '{1}' seconds. Please check if the page is loaded and the locator is valid.";

    public static string GetFormattedMessage(string template, params object[] args)
    {
        return string.Format(template, args);
    }
}