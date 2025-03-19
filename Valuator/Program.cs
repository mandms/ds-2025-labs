using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using Valuator.Repositories;
using Valuator.Services;

namespace Valuator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var connectionString = builder.Configuration.GetValue<string>("ConnectionString");

        builder.Services.AddSingleton<IConnectionMultiplexer>(options =>
            ConnectionMultiplexer.Connect(connectionString!));
        builder.Services.AddMvc(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));


        var redis = ConnectionMultiplexer.Connect(connectionString!);

        builder.Services.AddScoped<IValuatorRepository, ValuatorRepository>();

        builder.Services.AddScoped<IRabbitMQService, RabbitMQService>();

        builder.Services.AddDataProtection()
            .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys")
            .SetApplicationName("Valuator");

        builder.Services.AddCors();

        // Add services to the container.
        builder.Services.AddRazorPages();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}
