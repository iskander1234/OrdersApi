namespace OrdersApi.Presentation.DTOs;

public class CreateProductDto
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}