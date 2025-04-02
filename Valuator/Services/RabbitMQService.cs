using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Valuator.Services
{
    public class RabbitMQService : IRabbitMQService
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly ILogger<RabbitMQService> _logger;

        public RabbitMQService(IConfiguration configuration, ILogger<RabbitMQService> logger)
        {
            _logger = logger;

            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                UserName = configuration["RabbitMQ:Username"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest",
                Port = configuration.GetValue("RabbitMQ:Port", 5672),
            };

            try
            {
                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
                _logger.LogInformation("Подключение к RabbitMQ установлено");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка подключения к RabbitMQ");
                throw;
            }
        }

        public void SendMessage(object obj, CancellationTokenSource cts)
        {
            var message = JsonSerializer.Serialize(obj);
            Task.Factory.StartNew(() => ProduceAsync(cts.Token, message), cts.Token);
        }

        private async Task ProduceAsync(CancellationToken ct, string textId)
        {
            await DeclareTopologyAsync(_channel, ct);


                byte[] messageData = Encoding.UTF8.GetBytes(textId);

                await _channel.BasicPublishAsync(
                exchange: "calculate",
                routingKey: "rank",
                mandatory: false,
                body: messageData,
                cancellationToken: ct
                );

                await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }

        /// <summary>
        ///  Определяет топологию: producer -> exchange -> queue -> consumer.
        ///  В нашем случае соответствие 1:1 между exchange и queue, а routing key не используется.
        /// </summary>
        private async Task DeclareTopologyAsync(IChannel channel, CancellationToken ct)
        {
            await channel.ExchangeDeclareAsync(
                exchange: "valuator",
                type: ExchangeType.Direct,
                cancellationToken: ct
            );
            await channel.QueueDeclareAsync(
                queue: "calculate",
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: ct
            );
            await channel.QueueBindAsync(
                queue: "calculate",
                exchange: "valuator",
                routingKey: "rank",
                cancellationToken: ct);
        }
    }
}
