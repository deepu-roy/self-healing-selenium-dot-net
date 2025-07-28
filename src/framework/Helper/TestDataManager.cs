using AngleSharp.Text;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using TechTalk.SpecFlow;

namespace framework.Helper;

public static class TestDataManager
{
    private static ConcurrentDictionary<string, string?> _testData = new();

    public static ConcurrentDictionary<string, string?> TestData
    {
        get { return _testData; }
        private set { _testData = value; }
    }

    public static void AddToContext(ref ScenarioContext scenarioContext)
    {
        if (TestData == null)
            return;
        foreach (var data in TestData)
        {
            scenarioContext.Add(data.Key, data.Value);
        }
    }

    public static void Configure()
    {
        var environmentName = ConfigManager.GetConfiguration("environment");
        List<ConcurrentDictionary<string, string?>>? testDataCollection = null;

        using (StreamReader r = new("testdata.json"))
        {
            string json = r.ReadToEnd();
            if (json != null)
            {
                testDataCollection = JsonConvert.DeserializeObject<List<ConcurrentDictionary<string, string?>>>(json);
            }
        }

        if (testDataCollection != null)
        {
            foreach (var environment in testDataCollection)
            {
                if (environment != null)
                {
                    if (environment.TryGetValue("EnvironmentName", out string? actualEnvironmentName) && actualEnvironmentName == environmentName)
                    {
                        TestData = environment;
                        break;
                    }
                }
            }
        }
    }

    public static string GetTestData(string testDataName)
    {
        TestData.TryGetValue(testDataName, out var value);
        return value ?? string.Empty;
    }

    public static string Transform(ScenarioContext keyValuePairs, string valueToTransform)
    {
        if (valueToTransform == null || valueToTransform == string.Empty)
        {
            return valueToTransform ?? string.Empty;
        }
        string key = valueToTransform;
        string operation = string.Empty;
        string operand = string.Empty;
        string value = valueToTransform;
        dynamic result;
        if (key.StartsWith('['))
        {
            if (key.Contains('+') || key.Contains('-'))
            {
                operation = "+";

                if (key.Contains('-'))
                    operation = "-";
                var temp = key.Split(operation)[0];
                operand = key.Split(operation)[1];
                key = temp;
            }
            key = key.Replace("[", "");
            key = key.Replace("]", "");
            key = key.Trim();
        }

        if (key != string.Empty && keyValuePairs.ContainsKey(key))
        {
            keyValuePairs.TryGetValue<string>(key, out value);
        }

        if (operation != string.Empty && operand != string.Empty)
        {
            if (!Int32.TryParse(operand, out var actualOperand))
            {
                throw new ArgumentException($"Operand '{operand}' is not a valid integer.");
            }
            if (value.Contains('-')) // Check if the operand one is a date
            {
                var convertedValue = Convert.ToDateTime(value);
                var temp = operation switch
                {
                    "+" => convertedValue.AddDays(actualOperand),
                    "-" => convertedValue.AddDays(-actualOperand),
                    _ => throw new Exception("Operation not implemented in TestDataManager"),
                };
                result = $"{temp:yyyy-MM-dd}";
            }
            else // If not date checking if its a float or decimal
            {
                dynamic convertedValue;
                if (value.Contains('.'))
                {
                    if (!Double.TryParse(value, out double doubleValue))
                    {
                        throw new ArgumentException($"Value '{value}' is not a valid double.");
                    }
                    convertedValue = doubleValue;
                }
                else
                {
                    if (!Int32.TryParse(value, out int intValue))
                    {
                        throw new ArgumentException($"Value '{value}' is not a valid integer.");
                    }
                    convertedValue = intValue;
                }

                result = operation switch
                {
                    "+" => (convertedValue + actualOperand),
                    "-" => (convertedValue - actualOperand),
                    _ => throw new Exception("Operation not implemented in TestDataManager"),
                };
                if (result != null && result?.GetType() == typeof(double))
                {
                    result = string.Format($"{result:0.00}");
                }
            }
            value = result ?? value;
        }

        return value.ToString();
    }

    public static string TransformToValidNumber(string number)
    {
        string result = string.Empty;
        result = number.Trim();
        var length = number.Length;
        if (number.IndexOf('.') == length - 3)
        {
            result = String.Concat(number.Where(c => c != ','));
        }
        result = result.Replace(",", ".");
        result = String.Concat(result.Where(c => !c.IsWhiteSpaceCharacter()));
        return result;
    }
}