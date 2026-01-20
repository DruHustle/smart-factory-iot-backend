namespace SmartFactory.BuildingBlocks.EventBus.Models
{
    public abstract record IntegrationEvent
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime CreationDate { get; } = DateTime.UtcNow;
    }
}
