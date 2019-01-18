namespace Ordering.API.Application.IntegrationEvents.EventHandling
{
    using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Abstractions;
    using System.Threading.Tasks;
    using Events;
    using MediatR;
    using System;
    using Ordering.API.Application.Commands;
    using NServiceBus;

    public class OrderStockConfirmedIntegrationEventHandler : 
        IHandleMessages<OrderStockConfirmedIntegrationEvent>
    {
        private readonly IMediator _mediator;

        public OrderStockConfirmedIntegrationEventHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task Handle(OrderStockConfirmedIntegrationEvent @event, IMessageHandlerContext context)
        {
            var command = new SetStockConfirmedOrderStatusCommand(@event.OrderId);
            await _mediator.Send(command).ConfigureAwait(false);
        }
    }
}