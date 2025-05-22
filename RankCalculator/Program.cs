using RankCalculator;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetValue<string>("ConnectionString");

builder.Services.AddSignalR()
            .AddStackExchangeRedis("redis:6379");

builder.Services.AddScoped<IRankService, RankService>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
	ConnectionMultiplexer.Connect(connectionString!));

var app = builder.Build();

app.MapHub<RankHub>("/rankHub");

app.Run();
