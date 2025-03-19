using StackExchange.Redis;

namespace RankCalculator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            var connectionString = builder.Configuration.GetValue<string>("ConnectionString");

            builder.Services.AddHostedService<Worker>();

            builder.Services.AddSingleton<IConnectionMultiplexer>(options =>
                ConnectionMultiplexer.Connect(connectionString!));

            var host = builder.Build();
            host.Run();
        }
    }
}