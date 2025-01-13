using MediatR;

namespace OrdersApi.Application.Events;

public class OrderStatusChangedEvent : INotification
{
    public Guid OrderId { get; set; }
    public string OldStatus { get; set; }
    public string NewStatus { get; set; }

    public OrderStatusChangedEvent(Guid orderId, string oldStatus, string newStatus)
    {
        OrderId = orderId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}