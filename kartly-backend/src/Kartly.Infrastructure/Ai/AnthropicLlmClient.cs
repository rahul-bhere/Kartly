using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kartly.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Kartly.Infrastructure.Ai;

// -----------------------------------------------------------------------
// The ONLY class in the whole solution that knows Anthropic's Messages
// API shape (https://docs.anthropic.com — "Messages" + "Tool use").
// Everything else (AssistantService, AssistantController) talks to the
// generic ILlmClient interface instead, so swapping to OpenAI/Azure
// OpenAI/a local model later means writing one new class here — nothing
// in Application or API changes.
//
// SECURITY: the API key is read from configuration (user-secrets /
// environment variable — see backend README), NEVER hardcoded, and NEVER
// sent to or read by the frontend. The browser only ever talks to YOUR
// backend's /api/assistant/chat; your backend is the only thing that
// talks to Anthropic.
// -----------------------------------------------------------------------
public class AnthropicLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    private const string ApiVersion = "2023-06-01";
    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private const int MaxTokens = 1024;

    public AnthropicLlmClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException(
                "Anthropic:ApiKey is not configured. Set it via `dotnet user-secrets set \"Anthropic:ApiKey\" \"sk-ant-...\"` — see backend README's AI Assistant section.");
        _model = configuration["Anthropic:Model"] ?? "claude-sonnet-5";
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
            ["max_tokens"] = MaxTokens,
            ["system"] = systemPrompt,
            ["messages"] = BuildMessagesArray(messages),
            ["tools"] = BuildToolsArray(tools)
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("x-api-key", _apiKey);
        httpRequest.Headers.Add("anthropic-version", ApiVersion);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Anthropic API request failed ({(int)httpResponse.StatusCode}): {responseBody}");
        }

        return ParseResponse(responseBody);
    }

    private static JsonArray BuildMessagesArray(List<LlmMessage> messages)
    {
        var array = new JsonArray();

        foreach (var message in messages)
        {
            if (message.ToolCall is not null)
            {
                // Echoing the model's OWN previous tool call back to it,
                // as required by Anthropic's multi-turn tool-use protocol.
                var inputNode = JsonNode.Parse(
                    string.IsNullOrWhiteSpace(message.ToolCall.InputJson) ? "{}" : message.ToolCall.InputJson);

                array.Add(new JsonObject
                {
                    ["role"] = "assistant",
                    ["content"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "tool_use",
                            ["id"] = message.ToolCall.Id,
                            ["name"] = message.ToolCall.Name,
                            ["input"] = inputNode
                        }
                    }
                });
            }
            else if (message.ToolResultJson is not null)
            {
                // The result of executing that tool call, sent back as a
                // "user" turn per Anthropic's protocol — NOT actually
                // typed by the human user.
                array.Add(new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "tool_result",
                            ["tool_use_id"] = FindPrecedingToolUseId(messages, message),
                            ["content"] = message.ToolResultJson
                        }
                    }
                });
            }
            else
            {
                array.Add(new JsonObject
                {
                    ["role"] = message.Role,
                    ["content"] = message.Text
                });
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
                ["name"] = tool.Name,
                ["description"] = tool.Description,
                ["input_schema"] = JsonNode.Parse(schemaJson)
            });
        }
        return array;
    }

    private static LlmResponse ParseResponse(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var contentBlocks = doc.RootElement.GetProperty("content");

        string? text = null;
        LlmToolCall? toolCall = null;

        foreach (var block in contentBlocks.EnumerateArray())
        {
            var type = block.GetProperty("type").GetString();

            if (type == "text")
            {
                text = block.GetProperty("text").GetString();
            }
            else if (type == "tool_use")
            {
                toolCall = new LlmToolCall
                {
                    Id = block.GetProperty("id").GetString() ?? string.Empty,
                    Name = block.GetProperty("name").GetString() ?? string.Empty,
                    InputJson = block.GetProperty("input").GetRawText()
                };
            }
        }

        return new LlmResponse { TextContent = text, ToolCall = toolCall };
    }
}
