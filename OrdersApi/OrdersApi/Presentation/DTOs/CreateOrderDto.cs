namespace OrdersApi.Presentation.DTOs;

public class CreateOrderDto
{
    public string CustomerName { get; set; }
    public List<CreateProductDto> Products { get; set; }
}