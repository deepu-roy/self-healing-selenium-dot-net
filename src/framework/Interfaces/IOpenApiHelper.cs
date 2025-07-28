using System.Text.Json;

namespace framework.Interfaces;

public interface IOpenApiHelper
{
    Task<JsonDocument> GetInferenceAsync(string systemMessage, string prompt, OpenAI.Chat.ChatCompletionOptions? options = null);
}