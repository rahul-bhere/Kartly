using Kartly.API.Extensions;
using Kartly.Application.DTOs.Payments;
using Kartly.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kartly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    // POST /api/payments
    // SIMULATED — see PaymentService for exactly what that means.
    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Pay(CreatePaymentDto dto)
    {
        var payment = await _paymentService.SimulatePaymentAsync(User.GetUserId(), dto);
        return Ok(payment);
    }

    // GET /api/payments/order/{orderId}
    [HttpGet("order/{orderId:guid}")]
    public async Task<ActionResult<PaymentDto>> GetByOrderId(Guid orderId)
    {
        var payment = await _paymentService.GetByOrderIdAsync(orderId);
        return Ok(payment);
    }
}
