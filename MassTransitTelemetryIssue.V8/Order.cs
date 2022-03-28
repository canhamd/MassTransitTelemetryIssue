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
        private readonly ILogger<SubmitOrderConsumer> logger;

        public SubmitOrderConsumer(ILogger<SubmitOrderConsumer> logger)
        {
            this.logger = logger;
        }
        public async Task Consume(ConsumeContext<SubmitOrder> context)
        {
            logger.LogInformation("Consuming command");

            await context.RespondAsync<OrderAccepted>(new
            {
                OrderId = context.Message.OrderId
            });
        }
    }
}
