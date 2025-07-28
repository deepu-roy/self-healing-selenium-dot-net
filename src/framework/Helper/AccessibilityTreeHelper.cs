using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Newtonsoft.Json;
using System.Text;
using SpecFlow.Internal.Json;
using framework.Types;

namespace framework.Helper;

public static class AccessibilityTreeHelper
{
    // List of roles we consider interactive or important for context
    private static readonly HashSet<string> InteractiveRoles = new HashSet<string>
{
    "button", "link", "textField", "checkBox", "radioButton", "comboBox", "slider",
    "menuItem", "tab", "treeItem", "option", "searchBox", "form", "dialog",
    "alert", "region", "navigation", "toolbar", "table", "grid", "list", "heading"
};

    public static string? GetFullAccessibilityTree(this IWebDriver driver)
    {
        if (driver is not ChromeDriver chromeDriver)
        {
            throw new ArgumentException("Driver must be a ChromeDriver to use CDP commands.");
        }

        try
        {
            var cdpResponse = chromeDriver.ExecuteCdpCommand("Accessibility.getFullAXTree", new Dictionary<string, object>());

            if (cdpResponse is not Dictionary<string, object> responseDict)
            {
                throw new InvalidDataException("CDP command did not return a valid response.");
            }

            if (!responseDict.ContainsKey("nodes"))
            {
                throw new InvalidDataException("CDP response does not contain 'nodes' key.");
            }

            var rawJson = JsonConvert.SerializeObject(responseDict["nodes"]);
            var axNodes = JsonConvert.DeserializeObject<List<AxNode>>(rawJson);
            var flatTree = axNodes != null ? AccessibilityTreeHelper.BuildTreeFromFlatList(axNodes) : null;
            return flatTree != null ? AccessibilityTreeHelper.PruneAndFormatTree(flatTree, 0) : null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to retrieve accessibility tree.", ex);
        }
    }

    public static List<string?> GetRelevantHtmlSnippets(this IWebDriver driver)
    {
        var js = @"
        return Array.from(document.querySelectorAll(
            'a, button, input, select, textarea, [role], [tabindex]:not([tabindex=""-1""])'
        )).map(el => el.outerHTML);
    ";
        var results = (driver as IJavaScriptExecutor)?.ExecuteScript(js) as IReadOnlyCollection<object>;
        return results?.Select(r => r.ToString()).ToList() ?? new List<string?>();
    }

    private static AxNode BuildTreeFromFlatList(List<AxNode> flatNodes)
    {
        var nodeMap = flatNodes
            .GroupBy(n => n.NodeId)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var node in flatNodes)
        {
            foreach (var childId in node.ChildIds)
            {
                if (nodeMap.TryGetValue(childId, out var childNode))
                {
                    node.Children.Add(childNode);
                }
            }
        }
        return flatNodes[0];
    }

    private static string? PruneAndFormatTree(AxNode node, int level)
    {
        var childOutputs = new List<string>();
        foreach (var child in node.Children)
        {
            var formattedChild = PruneAndFormatTree(child, level + 1);
            if (!string.IsNullOrEmpty(formattedChild))
            {
                childOutputs.Add(formattedChild);
            }
        }

        bool isInteresting = InteractiveRoles.Contains(node.Role?.Value ?? string.Empty);
        bool hasInterestingChildren = childOutputs.Count != 0;

        if (!isInteresting && !hasInterestingChildren) return null;

        var indent = new string(' ', level * 2);
        var parts = new List<string>
        {
            $"- Role: {node.Role?.Value}"
        };

        if (!string.IsNullOrEmpty(node.Name?.Value))
            parts.Add($", Name: '{node.Name.Value}'");

        if (!string.IsNullOrEmpty(node.Description?.Value))
            parts.Add($", Desc: '{node.Description.Value}'");

        if (!string.IsNullOrEmpty(node.Value?.Value))
            parts.Add($", Value: '{node.Value.Value}'");

        var nodeOutput = indent + string.Join("", parts);

        var finalOutput = new StringBuilder();
        finalOutput.AppendLine(nodeOutput);
        foreach (var childOutput in childOutputs)
        {
            finalOutput.Append(childOutput);
        }

        return finalOutput.ToString();
    }
}