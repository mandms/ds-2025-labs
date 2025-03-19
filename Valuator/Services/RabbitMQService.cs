using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Valuator.Services
{
    public class RabbitMQService : IRabbitMQService
    {
        public void SendMessage(object obj, CancellationTokenSource cts)
        {
            var message = JsonSerializer.Serialize(obj);
            Task.Factory.StartNew(() => ProduceAsync(cts.Token, message), cts.Token);
        }

        private static async Task ProduceAsync(CancellationToken ct, string message)
        {
            // Установка соединения с RabbitMQ по адресу localhost:5672
            ConnectionFactory factory = new ConnectionFactory
            {
                HostName = "rabbitmq",
                UserName = "admin",
                Password = "123",
                Port = 5672,
            };

            await using IConnection connection = await factory.CreateConnectionAsync(ct);
            await using IChannel channel = await connection.CreateChannelAsync(null, ct);

            await DeclareTopologyAsync(channel, ct);

            byte[] messageData = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(
                exchange: "calculate",
                routingKey: "rank",
                mandatory: false,
                body: messageData,
                cancellationToken: ct
            );

            await connection.CloseAsync(ct);
        }

        /// <summary>
        ///  Определяет топологию: producer -> exchange -> queue -> consumer.
        ///  В нашем случае соответствие 1:1 между exchange и queue, а routing key не используется.
        /// </summary>
        private static async Task DeclareTopologyAsync(IChannel channel, CancellationToken ct)
        {
            await channel.ExchangeDeclareAsync(
                exchange: "calculate",
                type: ExchangeType.Direct,
                cancellationToken: ct
            );
            await channel.QueueDeclareAsync(
                queue: "valuator",
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: ct
            );
            await channel.QueueBindAsync(
                queue: "valuator",
                exchange: "calculate",
                routingKey: "rank",
                cancellationToken: ct);
        }
    }
}
