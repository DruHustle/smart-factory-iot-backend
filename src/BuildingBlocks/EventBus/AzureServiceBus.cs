using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace SmartFactory.BuildingBlocks.EventBus
{
    public class AzureServiceBus : IEventBus
    {
        private readonly ServiceBusClient _client;
        private readonly string _topicName = "smart_factory_event_bus";

        public AzureServiceBus(string connectionString)
        {
            _client = new ServiceBusClient(connectionString);
        }

        public async void Publish(IntegrationEvent @event)
        {
            var sender = _client.CreateSender(_topicName);
            var jsonMessage = JsonSerializer.Serialize(@event, @event.GetType());
            var message = new ServiceBusMessage(jsonMessage);
            
            await sender.SendMessageAsync(message);
        }

        public void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            // Implementation for Service Bus Processor to handle incoming messages
            // and dispatch to the appropriate handler (TH)
        }
    }
}
