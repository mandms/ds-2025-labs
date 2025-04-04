using RabbitMQ.Client;
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

        public void SendTextMessage(string textId, CancellationTokenSource cts)
		{
            var queueParams = new QueueParams 
            {
                Queue = "calculate",
                RoutingKey = "rank",
				Exchange = "valuator",
                ExchangeType = ExchangeType.Direct
			};

			Task.Factory.StartNew(() => ProduceAsync(cts.Token, queueParams, textId), cts.Token);
        }

        public void SendSimilarityMessage(bool similarity, string id, CancellationTokenSource cts)
        {
			var queueParams = new QueueParams
			{
				Queue = "similarity_calculated", //перенести в потребителя создание и тд очреди
				RoutingKey = "valuator.valuator.similarity.calculated",
				Exchange = "events",
                ExchangeType = ExchangeType.Topic
			};

            var similarityEventParams = new SimilarityEventParams
            {
                Similarity = similarity,
                Id = id
            };

			Task.Factory.StartNew(() => ProduceAsync(cts.Token, queueParams, similarityEventParams), cts.Token);
		}

        private async Task ProduceAsync(CancellationToken ct, QueueParams queueParams, object obj)
        {
            await DeclareTopologyAsync(_channel, queueParams, ct);

            byte[] messageData = JsonSerializer.SerializeToUtf8Bytes(obj);

            await _channel.BasicPublishAsync(
                exchange: queueParams.Exchange,
                routingKey: queueParams.RoutingKey,
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
        private async Task DeclareTopologyAsync(IChannel channel, QueueParams queueParams, CancellationToken ct)
        {
            await channel.ExchangeDeclareAsync(
                exchange: queueParams.Exchange,
                type: queueParams.ExchangeType,
                cancellationToken: ct
            );
            await channel.QueueDeclareAsync(
                queue: queueParams.Queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: ct
            );
            await channel.QueueBindAsync(
                queue: queueParams.Queue,
                exchange: queueParams.Exchange,
                routingKey: queueParams.RoutingKey,
                cancellationToken: ct);
        }

        struct QueueParams
        {
            public string Queue {  get; set; }
            public string Exchange {  get; set; }
            public string RoutingKey { get; set; }
            public string ExchangeType { get; set; }
		}

        struct SimilarityEventParams
        {
            public bool Similarity { get; set; }
            public string Id { get; set; }
        }
    }
}
