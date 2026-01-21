namespace SmartFactory.BuildingBlocks.EventBus.Models
{
    /// <summary>
    /// Represents the base class for all integration events in the system.
    /// Integration events are used to communicate state changes across different microservices.
    /// </summary>
    public abstract record IntegrationEvent
    {
        /// <summary>
        /// Gets the unique identifier for this specific event instance.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// Gets the timestamp when the event was created.
        /// </summary>
        public DateTime CreationDate { get; } = DateTime.UtcNow;
    }
}
