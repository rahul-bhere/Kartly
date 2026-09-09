namespace Kartly.Application.DTOs.Assistant;

public class ChatRequestDto
{
    public string Message { get; set; } = string.Empty;
    public List<ChatMessageDto> History { get; set; } = new();
}
