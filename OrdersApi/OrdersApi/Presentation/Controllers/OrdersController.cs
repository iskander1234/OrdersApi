using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersApi.Presentation.DTOs;
using OrdersApi.Services.InterfaceService;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // Создание заказа доступно User и Admin
    [Authorize(Roles = "User,Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var order = await _orderService.CreateOrder(dto);
        return CreatedAtAction(nameof(GetOrderById), new { orderId = order.OrderId }, order);
    }

    // Обновление заказа доступно User (только своих заказов) и Admin (любых заказов)
    [Authorize(Roles = "User,Admin")]
    [HttpPut("{orderId}")]
    public async Task<IActionResult> UpdateOrder(Guid orderId, [FromBody] UpdateOrderDto dto)
    {
        // Для User нужно добавить проверку, что заказ принадлежит текущему пользователю
        var role = User.FindFirst("role")?.Value;
        if (role == "User")
        {
            var order = await _orderService.GetOrderById(orderId);
            if (order == null || order.CustomerName != User.Identity?.Name)
                return Forbid();
        }

        await _orderService.UpdateOrder(orderId, dto);
        return NoContent();
    }

    // Получение заказа доступно User (только своих заказов) и Admin (любых заказов)
    [Authorize(Roles = "User,Admin")]
    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrderById(Guid orderId)
    {
        var order = await _orderService.GetOrderById(orderId);
        if (order == null) return NotFound();

        // Для User нужно проверить, что заказ принадлежит текущему пользователю
        var role = User.FindFirst("role")?.Value;
        if (role == "User" && order.CustomerName != User.Identity?.Name)
            return Forbid();

        return Ok(order);
    }

    // Получение списка заказов доступно только Admin
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] string status, decimal? minPrice, decimal? maxPrice)
    {
        var orders = await _orderService.GetOrders(status, minPrice, maxPrice);
        return Ok(orders);
    }

    // Мягкое удаление заказа доступно только Admin
    [Authorize(Roles = "Admin")]
    [HttpDelete("{orderId}")]
    public async Task<IActionResult> DeleteOrder(Guid orderId)
    {
        await _orderService.DeleteOrder(orderId);
        return NoContent();
    }
}
