using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using System.Text;

namespace RankCalculator
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConnectionMultiplexer _redis;
        private const string QueueName = "valuator";

        public Worker(ILogger<Worker> logger, IConnectionMultiplexer redis)
        {
            _logger = logger;
            _redis = redis;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunConsumerAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing messages");
                }
            }

            _logger.LogInformation("Worker stopped at: {time}", DateTimeOffset.Now);
        }

        private async Task RunConsumerAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "admin",
                Password = "123",
                Port = 5672,
            };

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await DeclareTopologyAsync(channel);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, eventArgs) => await ConsumeMessageAsync(channel, eventArgs);

            await channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer
            );

            _logger.LogInformation("Consumer started and waiting for messages...");

            // Keep the consumer running until cancellation is requested
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task ConsumeMessageAsync(IChannel channel, BasicDeliverEventArgs eventArgs)
        {
            try
            {
                _logger.LogInformation("Consuming message...");

                var messageJSON = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

                var message = JsonConvert.DeserializeAnonymousType(messageJSON, new { key = "", text = "" });

                if (message == null) return;

                double rank = CalculateRank(message.text);

                var db = _redis.GetDatabase();
                await db.StringSetAsync($"RANK-{message.key}", rank);

                await channel.BasicAckAsync(eventArgs.DeliveryTag, false);

                _logger.LogInformation("Message processed. Rank: {rank}", rank);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
            }
        }

        private async Task DeclareTopologyAsync(IChannel channel)
        {
            await channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            _logger.LogInformation("Topology declared for queue: {queue}", QueueName);
        }

        private static double CalculateRank(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            int totalChars = text.Length;
            int nonAlphabeticCount = text.Count(c => !char.IsLetter(c));

            return (double)nonAlphabeticCount / totalChars;
        }
    }
}