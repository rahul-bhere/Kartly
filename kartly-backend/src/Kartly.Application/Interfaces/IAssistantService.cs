using Kartly.Application.DTOs.Assistant;

namespace Kartly.Application.Interfaces;

public interface IAssistantService
{
    Task<ChatResponseDto> ChatAsync(Guid userId, bool isAdmin, ChatRequestDto request);
}
