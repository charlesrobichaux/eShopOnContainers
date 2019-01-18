namespace Payment.API.IntegrationEvents.EventHandling
{
    using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Abstractions;
    using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Events;
    using Microsoft.Extensions.Options;
    using NServiceBus;
    using Payment.API.IntegrationEvents.Events;
    using System.Threading.Tasks;

    public class OrderStatusChangedToStockConfirmedIntegrationEventHandler :
        IHandleMessages<OrderStatusChangedToStockConfirmedIntegrationEvent>
    {
        private readonly IEventBus _eventBus;
        private readonly PaymentSettings _settings;

        public OrderStatusChangedToStockConfirmedIntegrationEventHandler(IEventBus eventBus, 
            IOptionsSnapshot<PaymentSettings> settings)
        {
            _eventBus = eventBus;
            _settings = settings.Value;
        }         

        public async Task Handle(OrderStatusChangedToStockConfirmedIntegrationEvent @event, IMessageHandlerContext context)
        {
            //IntegrationEvent orderPaymentIntegrationEvent;

            ////Business feature comment:
            //// When OrderStatusChangedToStockConfirmed Integration Event is handled.
            //// Here we're simulating that we'd be performing the payment against any payment gateway
            //// Instead of a real payment we just take the env. var to simulate the payment 
            //// The payment can be successful or it can fail

            //if (_settings.PaymentSucceded)
            //{
            //    orderPaymentIntegrationEvent = new OrderPaymentSuccededIntegrationEvent(@event.OrderId);
            //}
            //else
            //{
            //    orderPaymentIntegrationEvent = new OrderPaymentFailedIntegrationEvent(@event.OrderId);
            //}

            //_eventBus.Publish(orderPaymentIntegrationEvent);
            if (_settings.PaymentSucceded)
            {
                await context.Publish(new OrderPaymentSuccededIntegrationEvent(@event.OrderId)).ConfigureAwait(false);
            }
            else
            {
                await context.Publish(new OrderPaymentFailedIntegrationEvent(@event.OrderId)).ConfigureAwait(false);
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}