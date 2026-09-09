using Kartly.API.Extensions;
using Kartly.Application.DTOs.Assistant;
using Kartly.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kartly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Any logged-in user (shopper or admin) can chat — the TOOLS
            // available differ by role (see AssistantService), which is
            // the actual security boundary, not this attribute alone.
public class AssistantController : ControllerBase
{
    private readonly IAssistantService _assistantService;

    public AssistantController(IAssistantService assistantService)
    {
        _assistantService = assistantService;
    }

    // POST /api/assistant/chat
    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponseDto>> Chat(ChatRequestDto request)
    {
        var response = await _assistantService.ChatAsync(User.GetUserId(), User.IsAdmin(), request);
        return Ok(response);
    }
}
