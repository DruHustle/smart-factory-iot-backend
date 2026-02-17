using Microsoft.Extensions.DependencyInjection;
using SmartFactory.BuildingBlocks.EventBus.Abstractions;
using SmartFactory.BuildingBlocks.EventBus.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace SmartFactory.BuildingBlocks.EventBus.Implementations
{
    public class RabbitMqEventBus : IEventBus, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _exchangeName = "smart_factory_event_bus";
        private readonly List<string> _consumerTags = new();

        public RabbitMqEventBus(string connectionString, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var factory = new ConnectionFactory
            {
                DispatchConsumersAsync = true
            };

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                factory.Uri = new Uri(connectionString);
            }

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(_exchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        }

        public async Task PublishAsync(IntegrationEvent @event)
        {
            var eventName = @event.GetType().Name;
            var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType());
            var props = _channel.CreateBasicProperties();
            props.ContentType = "application/json";
            props.DeliveryMode = 2;
            props.Type = eventName;

            _channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: eventName,
                mandatory: false,
                basicProperties: props,
                body: body);

            await Task.CompletedTask;
        }

        public void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = typeof(T).Name;
            var queueName = $"smart_factory.{eventName}.{typeof(TH).Name}".ToLowerInvariant();

            _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue: queueName, exchange: _exchangeName, routingKey: eventName);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (_, args) =>
            {
                try
                {
                    var message = JsonSerializer.Deserialize<T>(args.Body.ToArray());
                    if (message == null)
                    {
                        _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
                        return;
                    }

                    using var scope = _serviceProvider.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<TH>();
                    await handler.Handle(message);

                    _channel.BasicAck(args.DeliveryTag, multiple: false);
                }
                catch
                {
                    _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
                }
            };

            var tag = _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
            _consumerTags.Add(tag);
        }

        public void Dispose()
        {
            foreach (var tag in _consumerTags)
            {
                if (_channel.IsOpen)
                {
                    _channel.BasicCancel(tag);
                }
            }

            if (_channel.IsOpen)
            {
                _channel.Close();
            }

            if (_connection.IsOpen)
            {
                _connection.Close();
            }

            _channel.Dispose();
            _connection.Dispose();
        }
    }
}
