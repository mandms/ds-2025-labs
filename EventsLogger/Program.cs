namespace EventsLogger
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = Host.CreateApplicationBuilder(args);

			var connectionString = builder.Configuration.GetValue<string>("ConnectionString");

			builder.Services.AddHostedService<EventsConsumer>();

			var host = builder.Build();
			host.Run();
		}
	}
}