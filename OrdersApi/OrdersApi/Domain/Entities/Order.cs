namespace OrdersApi.Domain.Entities;

public class Order
{
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; }
    public string Status { get; set; } // pending, confirmed, cancelled
    public decimal TotalPrice { get; set; }
    public List<Product> Products { get; set; }
}