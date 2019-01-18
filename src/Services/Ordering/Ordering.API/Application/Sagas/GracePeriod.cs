using Microsoft.eShopOnContainers.Services.Ordering.API;
using Microsoft.Extensions.Options;
using NServiceBus;
using NServiceBus.Persistence.Sql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ordering.API.Application.IntegrationEvents.Events;
using Ordering.Domain.Events;

namespace Ordering.API.Application.Sagas
{
    public class GracePeriod : SqlSaga<GracePeriod.GracePeriodState>,
        IAmStartedByMessages<OrderStartedIntegrationEvent>,
        IHandleMessages<OrderStockConfirmedIntegrationEvent>,
        IHandleMessages<OrderStockRejectedIntegrationEvent>,
        IHandleMessages<OrderPaymentSuccededIntegrationEvent>,
        IHandleMessages<OrderPaymentFailedIntegrationEvent>,
        IHandleMessages<OrderStatusChangedToCancelledIntegrationEvent>,
        IHandleTimeouts<GracePeriodExpired>
    {
        private readonly OrderingSettings _settings;

        public GracePeriod(IOptions<OrderingSettings> settings)
        {

            this._settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        protected override void ConfigureMapping(IMessagePropertyMapper mapper)
        {
            mapper.ConfigureMapping<OrderStartedIntegrationEvent>(_ => _.Id);
            mapper.ConfigureMapping<OrderStockConfirmedIntegrationEvent>(_ => _.OrderId);
            mapper.ConfigureMapping<OrderStockRejectedIntegrationEvent>(_ => _.OrderId);
            mapper.ConfigureMapping<OrderPaymentSuccededIntegrationEvent>(_ => _.OrderId);
            mapper.ConfigureMapping<OrderPaymentFailedIntegrationEvent>(_ => _.OrderId);
            mapper.ConfigureMapping<OrderStatusChangedToCancelledIntegrationEvent>(_ => _.OrderId);
        }

        protected override string CorrelationPropertyName => nameof(GracePeriodState.OrderIdentifier);


        public async Task Handle(OrderStartedIntegrationEvent message, IMessageHandlerContext context)
        {
            Data.UserId = message.UserId;
            // We'll do the following actions at the same time, but asynchronous
            // - Grace Period
            // - ~~Verify buyer and payment method~~
            // - Verify if there is stock available
            var @event =
                new OrderStatusChangedToAwaitingValidationIntegrationEvent(Convert.ToInt32(message.UserId), "Awaiting", "",
                    new List<OrderStockItem>());

            await context.Publish(@event).ConfigureAwait(false);

            await RequestTimeout<GracePeriodExpired>(context, TimeSpan.FromMinutes(_settings.CheckUpdateTime)).ConfigureAwait(false);
        }
        public async Task Handle(OrderStockConfirmedIntegrationEvent message, IMessageHandlerContext context)
        {
            Data.StockConfirmed = true;
            await ContinueOrderingProcess(context).ConfigureAwait(false);
        }

        public async Task Timeout(GracePeriodExpired state, IMessageHandlerContext context)
        {
            Data.GracePeriodIsOver = true;
            await ContinueOrderingProcess(context).ConfigureAwait(false);
        }

        public Task Handle(OrderStockRejectedIntegrationEvent message, IMessageHandlerContext context)
        {
            // Another handler for OrderStockRejectedIntegrationEvent will update the status of the order
            MarkAsComplete();
            return Task.CompletedTask;
        }

        public Task Handle(OrderPaymentSuccededIntegrationEvent message, IMessageHandlerContext context)
        {
            // TODO: Publish this, but perhaps create a saga in stock as wel???
            //new OrderStatusChangedToPaidIntegrationEvent(Data.OrderIdentifier, )

            MarkAsComplete();

            return Task.CompletedTask;
        }

        public Task Handle(OrderPaymentFailedIntegrationEvent message, IMessageHandlerContext context)
        {
            // Another handler for OrderPaymentFailedIntegrationEvent will update the status of the order
            MarkAsComplete();
            return Task.CompletedTask;
        }

        public Task Handle(OrderStatusChangedToCancelledIntegrationEvent message, IMessageHandlerContext context)
        {
            // Nothing more to do; the saga is over
            MarkAsComplete();
            return Task.CompletedTask;
        }

        private async Task ContinueOrderingProcess(IMessageHandlerContext context)
        {
            if (Data.GracePeriodIsOver && Data.StockConfirmed)
            {
                var stockConfirmedEvent = new OrderStatusChangedToStockConfirmedIntegrationEvent(Data.OrderIdentifier, Data.OrderStatus, Data.BuyerName);
                await context.Publish(stockConfirmedEvent).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// State for our GracePeriod saga
        /// </summary>
        public class GracePeriodState : IContainSagaData
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }

            public int OrderIdentifier { get; set; }
            public string UserId { get; set; }
            public bool GracePeriodIsOver { get; set; }
            public bool StockConfirmed { get; set; }
            public string OrderStatus { get; set; }
            public string BuyerName { get; set; }
        }


    }
}
