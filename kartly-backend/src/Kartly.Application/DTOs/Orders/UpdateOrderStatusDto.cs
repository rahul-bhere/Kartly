namespace Kartly.Application.DTOs.Orders;

public class UpdateOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? CustomStatusLabel { get; set; }
}
