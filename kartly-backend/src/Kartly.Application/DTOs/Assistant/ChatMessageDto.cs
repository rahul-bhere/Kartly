namespace Kartly.Application.DTOs.Assistant;

// One turn in the conversation. The frontend keeps the running history in
// its own component state and resends it with every request (the backend
// itself is stateless between calls — see AssistantService for why).
public class ChatMessageDto
{
    // "user" or "assistant"
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
