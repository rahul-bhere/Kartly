namespace Kartly.Application.Interfaces;

// -----------------------------------------------------------------------
// Abstraction over "whatever LLM provider we call." AssistantService
// (business logic: which tool maps to which action) depends on THIS
// interface, never on Anthropic's SDK or HTTP shape directly — that
// detail lives in Kartly.Infrastructure/Ai/AnthropicLlmClient.cs. Swap
// providers (OpenAI, Azure OpenAI, local model) by writing a new
// Infrastructure class that implements this same interface.
// -----------------------------------------------------------------------
public interface ILlmClient
{
    Task<LlmResponse> SendAsync(
        string systemPrompt,
        List<LlmMessage> messages,
        List<LlmToolDefinition> tools,
        CancellationToken cancellationToken = default);
}

public class LlmMessage
{
    // "user" or "assistant"
    public string Role { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    // Present only on an "assistant" message that made a tool call, and
    // on the "user" message sent back afterward carrying the tool result.
    public LlmToolCall? ToolCall { get; set; }
    public string? ToolResultJson { get; set; }
}

public class LlmToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    // Raw JSON Schema for the tool's input, as Anthropic's Messages API expects it.
    public object InputSchema { get; set; } = new { };
}

public class LlmToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string InputJson { get; set; } = string.Empty;
}

public class LlmResponse
{
    // Populated when the model just wants to talk (no tool needed).
    public string? TextContent { get; set; }

    // Populated when the model wants to invoke exactly one tool. Kept to
    // one at a time deliberately — see AssistantService for why.
    public LlmToolCall? ToolCall { get; set; }
}
