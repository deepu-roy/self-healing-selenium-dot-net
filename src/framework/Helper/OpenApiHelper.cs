using framework.Configuration;
using framework.Interfaces;
using OpenAI.Chat;
using Serilog;
using System.Diagnostics;
using System.Text.Json;

namespace framework.Helper;

public class OpenApiHelper : IOpenApiHelper
{
    private static readonly Lazy<OpenApiHelper> _instance = new(() => new OpenApiHelper());
    private readonly ChatClient _chatClient;
    private readonly ILogger _logger;
    private readonly OpenApiConfig _config;

    public static OpenApiHelper Instance => _instance.Value;

    private OpenApiHelper()
    {
        _config = OpenApiConfig.Load();
        _chatClient = new ChatClient(model: _config.Model, apiKey: _config.ApiKey);
        _logger = ConfigManager.GetLogger();
    }

    public async Task<JsonDocument> GetInferenceAsync(string systemMessage, string prompt, ChatCompletionOptions? options = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N")[..8]; // Short unique ID for this request

        var messages = new ChatMessage[]
        {
            new SystemChatMessage(systemMessage),
            new UserChatMessage(prompt)
        };

        // Set default options with temperature if none provided
        options ??= new ChatCompletionOptions();
        
        // Set temperature to 0.9 for more creative/varied responses
        options.Temperature = 0.9f;

        // Log the start of the API call
        _logger.Information("OpenAI API Request Started - RequestId: {RequestId}, Model: {Model}, Temperature: {Temperature}, SystemMessageLength: {SystemMessageLength}, PromptLength: {PromptLength}",
            requestId, _config.Model, options.Temperature, systemMessage.Length, prompt.Length);

        try
        {
            var completion = await _chatClient.CompleteChatAsync(messages, options);
            stopwatch.Stop();

            var content = completion.Value.Content.FirstOrDefault()?.Text ?? string.Empty;

            if (string.IsNullOrEmpty(content))
            {
                _logger.Error("OpenAI API Request Failed - RequestId: {RequestId}, Reason: No content received from API, Duration: {Duration}ms",
                    requestId, stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException("No content received from OpenAI API.");
            }

            // Log successful API call with telemetry data
            LogApiTelemetry(requestId, completion.Value, stopwatch.ElapsedMilliseconds, content.Length);

            JsonDocument structuredResult = JsonDocument.Parse(content);

            _logger.Information("OpenAI API Request Completed Successfully - RequestId: {RequestId}, Duration: {Duration}ms",
                requestId, stopwatch.ElapsedMilliseconds);

            return structuredResult;
        }
        catch (JsonException ex)
        {
            stopwatch.Stop();
            _logger.Error(ex, "OpenAI API Request Failed - RequestId: {RequestId}, Reason: JSON Parse Error, Duration: {Duration}ms, ErrorMessage: {ErrorMessage}",
                requestId, stopwatch.ElapsedMilliseconds, ex.Message);
            throw new InvalidOperationException($"Failed to parse JSON response: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.Error(ex, "OpenAI API Request Failed - RequestId: {RequestId}, Reason: API Error, Duration: {Duration}ms, ErrorMessage: {ErrorMessage}",
                requestId, stopwatch.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }

    private void LogApiTelemetry(string requestId, ChatCompletion completion, long durationMs, int responseLength)
    {
        var usage = completion.Usage;
        var finishReason = completion.FinishReason;
        TelemetryStats.IncrementStats(usage.TotalTokenCount, durationMs);
        _logger.Information("OpenAI API Telemetry - RequestId: {RequestId}, " +
            "Model: {Model}, " +
            "Duration: {Duration}ms, " +
            "PromptTokens: {PromptTokens}, " +
            "CompletionTokens: {CompletionTokens}, " +
            "TotalTokens: {TotalTokens}, " +
            "ResponseLength: {ResponseLength}, " +
            "FinishReason: {FinishReason}, " +
            "Timestamp: {Timestamp}",
            requestId,
            _config.Model,
            durationMs,
            usage.InputTokenCount,
            usage.OutputTokenCount,
            usage.TotalTokenCount,
            responseLength,
            finishReason,
            DateTimeOffset.UtcNow);
    }

    public static class TelemetryStats
    {
        private static readonly object _lock = new();
        private static int _totalRequests = 0;
        private static int _totalTokens = 0;
        private static long _totalDuration = 0;

        public static void IncrementStats(int tokens, long duration)
        {
            lock (_lock)
            {
                _totalRequests++;
                _totalTokens += tokens;
                _totalDuration += duration;
            }
        }

        public static (int TotalRequests, int TotalTokens, long TotalDuration, double AverageTokensPerRequest, double AverageDurationPerRequest) GetStats()
        {
            lock (_lock)
            {
                return (
                    _totalRequests,
                    _totalTokens,
                    _totalDuration,
                    _totalRequests > 0 ? (double)_totalTokens / _totalRequests : 0,
                    _totalRequests > 0 ? (double)_totalDuration / _totalRequests : 0
                );
            }
        }
    }
}