using Azure.Messaging.ServiceBus;
using SmartFactory.BuildingBlocks.EventBus.Abstractions;
using SmartFactory.BuildingBlocks.EventBus.Models;
using System.Text.Json;

namespace SmartFactory.BuildingBlocks.EventBus.Implementations
{
    /// <summary>
    /// An implementation of the <see cref="IEventBus"/> using Azure Service Bus as the underlying message broker.
    /// </summary>
    public class AzureServiceBus : IEventBus
    {
        private readonly ServiceBusClient _client;
        private readonly string _topicName = "smart_factory_event_bus";

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureServiceBus"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string to the Azure Service Bus namespace.</param>
        public AzureServiceBus(string connectionString)
        {
            _client = new ServiceBusClient(connectionString);
        }

        /// <summary>
        /// Publishes an integration event to the Azure Service Bus topic.
        /// </summary>
        /// <param name="event">The integration event to publish.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task PublishAsync(IntegrationEvent @event)
        {
            // Create a sender for the specific topic
            var sender = _client.CreateSender(_topicName);
            
            // Serialize the event to JSON
            var jsonMessage = JsonSerializer.Serialize(@event, @event.GetType());
            
            // Create a Service Bus message with the serialized JSON content
            var message = new ServiceBusMessage(jsonMessage);
            
            // Send the message to the topic
            await sender.SendMessageAsync(message);
        }

        /// <summary>
        /// Subscribes a handler to a specific type of integration event.
        /// </summary>
        /// <typeparam name="T">The type of integration event to subscribe to.</typeparam>
        /// <typeparam name="TH">The type of the handler that will process the event.</typeparam>
        /// <remarks>
        /// The current implementation is a placeholder. In a full implementation, this would
        /// set up a Service Bus Processor to listen for messages on a subscription.
        /// </remarks>
        public void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            // Implementation for Service Bus Processor would go here
            // This would typically involve creating a ServiceBusProcessor for a subscription
            // and registering the handler to process incoming messages.
        }
    }
}
