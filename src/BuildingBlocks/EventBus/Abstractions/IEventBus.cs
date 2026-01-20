using SmartFactory.BuildingBlocks.EventBus.Models;

namespace SmartFactory.BuildingBlocks.EventBus.Abstractions
{
    public interface IEventBus
    {
        Task PublishAsync(IntegrationEvent @event);
        void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;
    }

    public interface IIntegrationEventHandler<in TIntegrationEvent>
        where TIntegrationEvent : IntegrationEvent
    {
        Task Handle(TIntegrationEvent @event);
    }
}
