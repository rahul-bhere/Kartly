using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kartly.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Kartly.Infrastructure.Ai;

// FREE ALTERNATIVE to AnthropicLlmClient. Groq (https://console.groq.com)
// offers a genuinely free tier with fast inference and OpenAI-compatible
// tool-calling, which is why this implements the SAME ILlmClient
// interface — swap which one is registered via "Llm:Provider" in
// appsettings (see Infrastructure/DependencyInjection.cs) without
// AssistantService or the controller knowing anything changed. This is
// the DEFAULT provider for this project because it's free.
public class GroqLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    private const string Endpoint = "https://api.groq.com/openai/v1/chat/completions";

    public GroqLlmClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Groq:ApiKey"]
            ?? throw new InvalidOperationException(
                "Groq:ApiKey is not configured. Get a free key at console.groq.com, then run `dotnet user-secrets set \"Groq:ApiKey\" \"gsk_...\"` — see backend README's AI Assistant section.");
        // Default model: openai/gpt-oss-20b. Groq periodically deprecates
        // models with short notice (e.g. llama-3.3-70b-versatile and
        // llama-3.1-8b-instant were both retired in June 2026) — if this
        // starts returning a 404 "model_not_found" again, check the
        // current list at https://console.groq.com/docs/models (or
        // `GET https://api.groq.com/openai/v1/models` with your key) and
        // override via `dotnet user-secrets set "Groq:Model" "<id>"`
        // rather than editing this file.
        _model = configuration["Groq:Model"] ?? "openai/gpt-oss-20b";
    }

    public async Task<LlmResponse> SendAsync(
        string systemPrompt,
        List<LlmMessage> messages,
        List<LlmToolDefinition> tools,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new JsonObject
        {
            ["model"] = _model,
            ["messages"] = BuildMessagesArray(systemPrompt, messages),
            ["tools"] = BuildToolsArray(tools),
            ["tool_choice"] = "auto"
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Groq API request failed ({(int)httpResponse.StatusCode}): {responseBody}");
        }

        return ParseResponse(responseBody);
    }

    private static JsonArray BuildMessagesArray(string systemPrompt, List<LlmMessage> messages)
    {
        var array = new JsonArray { new JsonObject { ["role"] = "system", ["content"] = systemPrompt } };

        foreach (var message in messages)
        {
            if (message.ToolCall is not null)
            {
                array.Add(new JsonObject
                {
                    ["role"] = "assistant",
                    ["content"] = null,
                    ["tool_calls"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["id"] = message.ToolCall.Id,
                            ["type"] = "function",
                            ["function"] = new JsonObject
                            {
                                ["name"] = message.ToolCall.Name,
                                ["arguments"] = string.IsNullOrWhiteSpace(message.ToolCall.InputJson) ? "{}" : message.ToolCall.InputJson
                            }
                        }
                    }
                });
            }
            else if (message.ToolResultJson is not null)
            {
                var toolCallId = FindPrecedingToolUseId(messages, message);
                array.Add(new JsonObject
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = toolCallId,
                    ["content"] = message.ToolResultJson
                });
            }
            else
            {
                array.Add(new JsonObject { ["role"] = message.Role, ["content"] = message.Text });
            }
        }

        return array;
    }

    private static string FindPrecedingToolUseId(List<LlmMessage> messages, LlmMessage current)
    {
        var index = messages.IndexOf(current);
        for (var i = index - 1; i >= 0; i--)
        {
            if (messages[i].ToolCall is not null) return messages[i].ToolCall!.Id;
        }
        throw new InvalidOperationException("Tool result message has no preceding tool call to match.");
    }

    private static JsonArray BuildToolsArray(List<LlmToolDefinition> tools)
    {
        var array = new JsonArray();
        foreach (var tool in tools)
        {
            var schemaJson = JsonSerializer.Serialize(tool.InputSchema);
            array.Add(new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["parameters"] = JsonNode.Parse(schemaJson)
                }
            });
        }
        return array;
    }

    private static LlmResponse ParseResponse(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var message = doc.RootElement.GetProperty("choices")[0].GetProperty("message");

        string? text = message.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == JsonValueKind.String
            ? contentProp.GetString()
            : null;

        LlmToolCall? toolCall = null;
        if (message.TryGetProperty("tool_calls", out var toolCallsProp) && toolCallsProp.GetArrayLength() > 0)
        {
            var firstCall = toolCallsProp[0];
            var function = firstCall.GetProperty("function");
            toolCall = new LlmToolCall
            {
                Id = firstCall.GetProperty("id").GetString() ?? string.Empty,
                Name = function.GetProperty("name").GetString() ?? string.Empty,
                InputJson = function.GetProperty("arguments").GetString() ?? "{}"
            };
        }

        return new LlmResponse { TextContent = text, ToolCall = toolCall };
    }
}
