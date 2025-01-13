using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OrdersApi.Domain.Entities;
using OrdersApi.Infrastructure.Data;
using OrdersApi.Presentation.DTOs;
using OrdersApi.Services;

namespace OrdersApi.Test;

public class Tests
{
    private OrderService _orderService;
    private Mock<ILogger<OrderService>> _loggerMock;
    private OrderDbContext _dbContext;

    [SetUp]
    public void Setup()
    {
        // Настраиваем in-memory database
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new OrderDbContext(options);
        _loggerMock = new Mock<ILogger<OrderService>>();
        _orderService = new OrderService(_dbContext, _loggerMock.Object);
    }

    [Test]
    public async Task CreateOrder_ShouldCreateOrderSuccessfully()
    {
        // Arrange
        var createOrderDto = new CreateOrderDto
        {
            CustomerName = "Test User",
            Products = new List<CreateProductDto>
            {
                new CreateProductDto { Name = "Product1", Price = 10, Quantity = 2 },
                new CreateProductDto { Name = "Product2", Price = 20, Quantity = 1 }
            }
        };

        // Act
        var result = await _orderService.CreateOrder(createOrderDto);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test User", result.CustomerName);
        Assert.AreEqual(2, result.Products.Count);
        Assert.AreEqual(40, result.TotalPrice);
    }

    [Test]
    public async Task GetOrderById_ShouldReturnOrder_WhenOrderExists()
    {
        // Arrange
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            CustomerName = "Test User",
            Status = "pending",
            TotalPrice = 100,
            Products = new List<Product>
            {
                new Product { ProductId = Guid.NewGuid(), Name = "Product1", Price = 50, Quantity = 1 }
            }
        };
        await _dbContext.Orders.AddAsync(order);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _orderService.GetOrderById(order.OrderId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(order.CustomerName, result.CustomerName);
        Assert.AreEqual(1, result.Products.Count);
    }

    [Test]
    public async Task UpdateOrder_ShouldUpdateStatusSuccessfully()
    {
        // Arrange
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            CustomerName = "Test User",
            Status = "pending",
            TotalPrice = 100
        };
        await _dbContext.Orders.AddAsync(order);
        await _dbContext.SaveChangesAsync();

        var updateOrderDto = new UpdateOrderDto { Status = "confirmed" };

        // Act
        var result = await _orderService.UpdateOrder(order.OrderId, updateOrderDto);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("confirmed", result.Status);
    }

    [Test]
    public async Task DeleteOrder_ShouldMarkOrderAsDeleted()
    {
        // Arrange
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            CustomerName = "Test User",
            Status = "pending",
            TotalPrice = 100
        };
        await _dbContext.Orders.AddAsync(order);
        await _dbContext.SaveChangesAsync();

        // Act
        await _orderService.DeleteOrder(order.OrderId);

        // Assert
        var deletedOrder = await _dbContext.Orders.FindAsync(order.OrderId);
        Assert.IsNotNull(deletedOrder);
        Assert.AreEqual("deleted", deletedOrder.Status);
    }

    [Test]
    public async Task GetOrders_ShouldFilterOrdersCorrectly()
    {
        // Arrange
        var orders = new List<Order>
        {
            new Order
            {
                OrderId = Guid.NewGuid(),
                CustomerName = "User1",
                Status = "pending",
                TotalPrice = 50
            },
            new Order
            {
                OrderId = Guid.NewGuid(),
                CustomerName = "User2",
                Status = "confirmed",
                TotalPrice = 150
            }
        };
        await _dbContext.Orders.AddRangeAsync(orders);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _orderService.GetOrders("pending", null, 100);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("User1", result.First().CustomerName);
    }
}
