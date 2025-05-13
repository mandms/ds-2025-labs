using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace RankCalculator
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IShardManager _shardManager;
		private readonly IConnection _connection;
        private QueueParams queueParams = new()
        {
            Queue = "calculate",
            Exchange = "valuator",
            ExchangeType = ExchangeType.Direct,
            RoutingKey = "rank"
        };

		public Worker(IConfiguration configuration, ILogger<Worker> logger, IShardManager shardManager)
        {
            _logger = logger;
            _shardManager = shardManager;

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
				_logger.LogInformation("Подключение к RabbitMQ установлено");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Ошибка подключения к RabbitMQ");
				throw;
			}
		}

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker started at: {time}", DateTimeOffset.Now);

            try
            {
                await RunConsumerAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing messages");
            }

            _logger.LogInformation("Worker stopped at: {time}", DateTimeOffset.Now);
        }

        private async Task RunConsumerAsync(CancellationToken stoppingToken)
        {
			IChannel channel = await _connection.CreateChannelAsync();

			await DeclareTopologyAsync(channel, stoppingToken);
			var consumer = new AsyncEventingBasicConsumer(channel);
			consumer.ReceivedAsync += async (_, eventArgs) => await ConsumeMessageAsync(eventArgs, channel);

			await channel.BasicConsumeAsync(
				queue: queueParams.Queue,
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

        private async Task ConsumeMessageAsync(BasicDeliverEventArgs eventArgs, IChannel channel)
        {
            _logger.LogInformation("Consuming message...");

            string key = Encoding.UTF8.GetString(eventArgs.Body.ToArray()).Trim('\"');
			_shardManager.SetShard(key);

            string text = Convert.ToString(_shardManager.GetText(key));

            var rank = CalculateRank(text!);

            _shardManager.SetToRegion(key, rank, "RANK-");

            var rankEventProps = new RankEventProps
            {
                Id = key,
                Rank = rank
            };

            await ProduceAsync(channel, rankEventProps, new CancellationToken()); //возможно ошибка из-за токена

			await channel.BasicAckAsync(eventArgs.DeliveryTag, false);

            _logger.LogInformation("key: {key} text: {text}", key, text);

            _logger.LogInformation("Message processed. Rank: {rank}", rank);
        }

        private static double CalculateRank(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            int totalChars = text.Length;
            int nonAlphabeticCount = text.Count(c => !char.IsLetter(c));

            return (double)nonAlphabeticCount / totalChars;
        }

		private async Task DeclareTopologyAsync(IChannel channel, CancellationToken ct)
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

		private async Task ProduceAsync(IChannel channel, RankEventProps rankEventProps, CancellationToken ct)
        {
			byte[] messageData = JsonSerializer.SerializeToUtf8Bytes(rankEventProps);

			await channel.BasicPublishAsync(
				exchange: "events",
				routingKey: "valuator.events_logger.rank.calculated",
				mandatory: false,
				body: messageData,
				cancellationToken: ct
			);
		}

        struct RankEventProps
        {
            public double Rank { get; set; }
            public string Id { get; set; }
        }

		struct QueueParams
		{
			public string Queue { get; set; }
			public string Exchange { get; set; }
			public string RoutingKey { get; set; }
			public string ExchangeType { get; set; }
		}
	}
}