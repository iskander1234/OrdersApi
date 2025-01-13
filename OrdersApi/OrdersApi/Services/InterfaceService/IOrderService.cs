using OrdersApi.Domain.Entities;
using OrdersApi.Presentation.DTOs;

namespace OrdersApi.Services.InterfaceService;

public interface IOrderService
{
    Task<Order> CreateOrder(CreateOrderDto dto);
    Task<Order> UpdateOrder(Guid orderId, UpdateOrderDto dto);
    Task<Order> GetOrderById(Guid orderId);
    Task<List<Order>> GetOrders(string status, decimal? minPrice, decimal? maxPrice);
    Task DeleteOrder(Guid orderId);
}