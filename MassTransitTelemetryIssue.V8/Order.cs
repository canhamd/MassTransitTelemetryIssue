using MassTransit;

namespace MassTransitTelemetryIssue.V8
{
    public interface SubmitOrder
    {
        public Guid OrderId { get; }

    }

    public interface OrderAccepted
    {
        public Guid OrderId { get; }

    }

    public class SubmitOrderConsumer : IConsumer<SubmitOrder>
    {
        public async Task Consume(ConsumeContext<SubmitOrder> context)
        {
            await context.RespondAsync<OrderAccepted>(new
            {
                OrderId = context.Message.OrderId
            });
        }
    }
}
