using SmartFactory.BuildingBlocks.EventBus.Models;

namespace SmartFactory.BuildingBlocks.EventBus.Abstractions
{
    /// <summary>
    /// Defines the contract for an Event Bus that facilitates communication between microservices
    /// using the Publish-Subscribe pattern.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Publishes an integration event to the event bus.
        /// </summary>
        /// <param name="event">The integration event to publish.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task PublishAsync(IntegrationEvent @event);

        /// <summary>
        /// Subscribes a handler to a specific type of integration event.
        /// </summary>
        /// <typeparam name="T">The type of integration event to subscribe to.</typeparam>
        /// <typeparam name="TH">The type of the handler that will process the event.</typeparam>
        void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;
    }

    /// <summary>
    /// Defines the contract for a handler that processes a specific type of integration event.
    /// </summary>
    /// <typeparam name="TIntegrationEvent">The type of integration event to handle.</typeparam>
    public interface IIntegrationEventHandler<in TIntegrationEvent>
        where TIntegrationEvent : IntegrationEvent
    {
        /// <summary>
        /// Handles the specified integration event.
        /// </summary>
        /// <param name="event">The integration event to handle.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task Handle(TIntegrationEvent @event);
    }
}
