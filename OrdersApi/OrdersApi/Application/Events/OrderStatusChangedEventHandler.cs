using MediatR;

namespace OrdersApi.Application.Events;

public class OrderStatusChangedEventHandler : INotificationHandler<OrderStatusChangedEvent>
{
    private readonly ILogger<OrderStatusChangedEventHandler> _logger;

    public OrderStatusChangedEventHandler(ILogger<OrderStatusChangedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(OrderStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Событие: Статус заказа {OrderId} изменился с {OldStatus} на {NewStatus}",
            notification.OrderId,
            notification.OldStatus,
            notification.NewStatus
        );

        // Здесь можно добавить отправку уведомлений или другие действия
        return Task.CompletedTask;
    }
}
