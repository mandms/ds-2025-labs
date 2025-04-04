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
            var publishParams = new PublishParams 
            {
                RoutingKey = "rank",
				Exchange = "valuator"
			};

			Task.Factory.StartNew(() => ProduceAsync(cts.Token, publishParams, textId), cts.Token);
        }

        public void SendSimilarityMessage(bool similarity, string id, CancellationTokenSource cts)
        {
			var publishParams = new PublishParams
			{
				RoutingKey = "valuator.events_logger.similarity.calculated",
				Exchange = "events"
			};

            var similarityEventParams = new SimilarityEventParams
            {
                Similarity = similarity,
                Id = id
            };

			Task.Factory.StartNew(() => ProduceAsync(cts.Token, publishParams, similarityEventParams), cts.Token);
		}

        private async Task ProduceAsync(CancellationToken ct, PublishParams publishParams, object obj)
        {
            byte[] messageData = JsonSerializer.SerializeToUtf8Bytes(obj);

            await _channel.BasicPublishAsync(
                exchange: publishParams.Exchange,
                routingKey: publishParams.RoutingKey,
                mandatory: false,
                body: messageData,
                cancellationToken: ct
            );

            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }

        struct PublishParams
        {
            public string Exchange {  get; set; }
            public string RoutingKey { get; set; }
		}

        struct SimilarityEventParams
        {
            public bool Similarity { get; set; }
            public string Id { get; set; }
        }
    }
}
