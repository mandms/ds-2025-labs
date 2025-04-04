using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text.Json;

namespace EventsLogger
{
	public class EventsConsumer : BackgroundService
	{
		private readonly ILogger<EventsConsumer> _logger;
		private readonly IConnection _connection;

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
				var rankTask = RunRankConsumerAsync(stoppingToken);
				var similarityTask = RunSimilarityConsumerAsync(stoppingToken);

				await Task.WhenAll(rankTask, similarityTask);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while processing messages");
			}

			_logger.LogInformation("Worker stopped at: {time}", DateTimeOffset.Now);
		}

		private async Task RunRankConsumerAsync(CancellationToken stoppingToken)
		{
			try
			{
				IChannel channel = await _connection.CreateChannelAsync();

				await DeclareTopologyAsync(channel, "similarity_calculated");
				var consumer = new AsyncEventingBasicConsumer(channel);
				consumer.ReceivedAsync += async (_, eventArgs) => await ConsumeSimilarityMessageAsync(eventArgs, channel);

				await channel.BasicConsumeAsync(
					queue: "similarity_calculated",
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

		private async Task RunSimilarityConsumerAsync(CancellationToken stoppingToken)
		{
			try
			{
				IChannel channel = await _connection.CreateChannelAsync();

				await DeclareTopologyAsync(channel, "rank_calculated");
				var consumer = new AsyncEventingBasicConsumer(channel);
				consumer.ReceivedAsync += async (_, eventArgs) => await ConsumeRankMessageAsync(eventArgs, channel);

				await channel.BasicConsumeAsync(
					queue: "rank_calculated",
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

		private async Task ConsumeRankMessageAsync(BasicDeliverEventArgs eventArgs, IChannel channel)
		{
			RankEventProps rankEventProps = await JsonSerializer.DeserializeAsync<RankEventProps>(new MemoryStream(eventArgs.Body.ToArray()));

			_logger.LogInformation("Event type: RankCalculated \n " +
				"EntityId: {id} \n " +
				"Rank: {rank}", rankEventProps.Id, rankEventProps.Rank);
		}

		private async Task ConsumeSimilarityMessageAsync(BasicDeliverEventArgs eventArgs, IChannel channel)
		{
			SimilarityEventParams similarityEventProps = await JsonSerializer.DeserializeAsync<SimilarityEventParams>(new MemoryStream(eventArgs.Body.ToArray()));

			_logger.LogInformation("Event type: SimilarityCalculated \n " +
				"EntityId: {id} \n " +
				"Similarity: {similarity}", similarityEventProps.Id, similarityEventProps.Similarity);
		}

		private async Task DeclareTopologyAsync(IChannel channel, string queueName)
		{
			await channel.QueueDeclareAsync(
				queue: queueName,
				durable: true,
				exclusive: false,
				autoDelete: false
			);
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
	}
}
