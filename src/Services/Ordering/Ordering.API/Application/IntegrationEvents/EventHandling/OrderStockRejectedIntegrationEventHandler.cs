using System;

namespace Ordering.API.Application.IntegrationEvents.EventHandling
{
    using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Abstractions;
    using System.Threading.Tasks;
    using Events;
    using System.Linq;
    using MediatR;
    using Ordering.API.Application.Commands;
    using NServiceBus;

    public class OrderStockRejectedIntegrationEventHandler : IHandleMessages<OrderStockRejectedIntegrationEvent>
    {
        private readonly IMediator _mediator;

        public OrderStockRejectedIntegrationEventHandler(IMediator mediator) => _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

        public async Task Handle(OrderStockRejectedIntegrationEvent @event, IMessageHandlerContext context)
        {
            var orderStockRejectedItems = @event.OrderStockItems
                .FindAll(c => !c.HasStock)
                .Select(c => c.ProductId)
                .ToList();

            var command = new SetStockRejectedOrderStatusCommand(@event.OrderId, orderStockRejectedItems);
            await _mediator.Send(command).ConfigureAwait(false);
        }
    }
}