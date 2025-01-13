using Microsoft.EntityFrameworkCore;
using OrdersApi.Domain.Entities;
using OrdersApi.Infrastructure.Data;
using OrdersApi.Presentation.DTOs;
using OrdersApi.Services.InterfaceService;

namespace OrdersApi.Services;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _dbContext;
    private readonly ILogger<OrderService> _logger;

    public OrderService(OrderDbContext dbContext, ILogger<OrderService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Order> CreateOrder(CreateOrderDto dto)
    {
        _logger.LogInformation("Creating a new order for customer {CustomerName}", dto.CustomerName);

        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            CustomerName = dto.CustomerName,
            Status = "pending",
            Products = dto.Products.Select(p => new Product
            {
                ProductId = Guid.NewGuid(),
                Name = p.Name,
                Price = p.Price,
                Quantity = p.Quantity
            }).ToList(),
            TotalPrice = dto.Products.Sum(p => p.Price * p.Quantity)
        };

        await _dbContext.Orders.AddAsync(order);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} created successfully", order.OrderId);

        return order;
    }

    public async Task<Order> UpdateOrder(Guid orderId, UpdateOrderDto dto)
    {
        ValidateOrderStatus(dto.Status);

        var order = await _dbContext.Orders.FindAsync(orderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found", orderId);
            throw new KeyNotFoundException($"Order with ID {orderId} not found");
        }

        order.Status = dto.Status;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} updated successfully", orderId);

        return order;
    }

    public async Task<Order> GetOrderById(Guid orderId)
    {
        var order = await _dbContext.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found", orderId);
        }

        return order;
    }

    public async Task<List<Order>> GetOrders(string status, decimal? minPrice, decimal? maxPrice)
    {
        var query = _dbContext.Orders.AsNoTracking().Where(o => o.Status != "deleted");

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);

        if (minPrice.HasValue)
            query = query.Where(o => o.TotalPrice >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(o => o.TotalPrice <= maxPrice.Value);

        var orders = await query.ToListAsync();
        _logger.LogInformation("Retrieved {OrderCount} orders", orders.Count);

        return orders;
    }

    public async Task DeleteOrder(Guid orderId)
    {
        var order = await _dbContext.Orders.FindAsync(orderId);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found", orderId);
            throw new KeyNotFoundException($"Order with ID {orderId} not found");
        }

        order.Status = "deleted";
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} marked as deleted", orderId);
    }

    private void ValidateOrderStatus(string status)
    {
        var validStatuses = new[] { "pending", "confirmed", "cancelled", "deleted" };
        if (!validStatuses.Contains(status))
        {
            _logger.LogError("Invalid order status: {Status}", status);
            throw new ArgumentException($"Invalid order status: {status}");
        }
    }
}
