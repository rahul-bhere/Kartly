namespace Kartly.Application.DTOs.Assistant;

public class ChatResponseDto
{
    // The assistant's natural-language reply, shown in the chat bubble.
    public string Reply { get; set; } = string.Empty;

    // A short audit trail of what was actually executed against the
    // database this turn (e.g. "Created product 'Wireless Mouse'"),
    // shown as small tags under the reply so the admin can see — at a
    // glance — that a real CRUD action happened, not just talk.
    public List<string> ActionsPerformed { get; set; } = new();
}
