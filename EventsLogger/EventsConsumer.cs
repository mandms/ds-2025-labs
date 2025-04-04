using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text.Json;

namespace EventsLogger
{
	public class EventsConsumer : BackgroundService
	{
		private readonly ILogger<EventsConsumer> _logger;
		private readonly IConnection _connection;
		private readonly QueueParams _queueParams = new()
		{
			Queue = "events_bus",
			RoutingKey = "valuator.events_logger.#", //откуда или куда? valuator.valuator.similarity.calculated
			Exchange = "events",
			ExchangeType = ExchangeType.Topic,
		};

		public EventsConsumer(IConfiguration configuration, ILogger<EventsConsumer> logger)
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
			try
			{
				IChannel channel = await _connection.CreateChannelAsync();

				await DeclareTopologyAsync(channel, stoppingToken);
				var consumer = new AsyncEventingBasicConsumer(channel);
				consumer.ReceivedAsync += async (_, eventArgs) => await ConsumeMessageAsync(eventArgs, channel);

				await channel.BasicConsumeAsync(
					queue: _queueParams.Queue,
					autoAck: false,
					consumer: consumer
				);
			}
			catch (Exception e)
			{
				_logger.LogError(e.Message);
			};

			// Keep the consumer running until cancellation is requested
			while (!stoppingToken.IsCancellationRequested)
			{
				await Task.Delay(1000, stoppingToken);
			}
		}

		private async Task ConsumeRankAsync(BasicDeliverEventArgs eventArgs, IChannel channel)
		{
			RankEventProps rankEventProps = await JsonSerializer.DeserializeAsync<RankEventProps>(new MemoryStream(eventArgs.Body.ToArray()));
			
			_logger.LogInformation("Event type: RankCalculated \n " +
				"EntityId: {id} \n " +
				"Rank: {rank}", rankEventProps.Id, rankEventProps.Rank);
		}

		private async Task ConsumeSimilarityAsync(BasicDeliverEventArgs eventArgs, IChannel channel)
		{
			SimilarityEventParams similarityEventProps = await JsonSerializer.DeserializeAsync<SimilarityEventParams>(new MemoryStream(eventArgs.Body.ToArray()));

			_logger.LogInformation("Event type: SimilarityCalculated \n " +
				"EntityId: {id} \n " +
				"Similarity: {similarity}", similarityEventProps.Id, similarityEventProps.Similarity);
		}

		private async Task ConsumeMessageAsync(BasicDeliverEventArgs eventArgs, IChannel channel)
		{
			if (eventArgs.RoutingKey.EndsWith("similarity.calculated"))
			{
				await ConsumeSimilarityAsync(eventArgs, channel);
			}

			if (eventArgs.RoutingKey.EndsWith("rank.calculated"))
			{
				await ConsumeRankAsync(eventArgs, channel);
			}
		}

		private async Task DeclareTopologyAsync(IChannel channel, CancellationToken ct)
		{
			await channel.ExchangeDeclareAsync(
				exchange: _queueParams.Exchange,
				type: _queueParams.ExchangeType,
				cancellationToken: ct
			);
			await channel.QueueDeclareAsync(
				queue: _queueParams.Queue,
				durable: true,
				exclusive: false,
				autoDelete: false,
				cancellationToken: ct
			);
			await channel.QueueBindAsync(
				queue: _queueParams.Queue,
				exchange: _queueParams.Exchange,
				routingKey: _queueParams.RoutingKey,
				cancellationToken: ct);
		}

		struct RankEventProps
		{
			public double Rank { get; set; }
			public string Id { get; set; }
		}

		struct SimilarityEventParams
		{
			public bool Similarity { get; set; }
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
