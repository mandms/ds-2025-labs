using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Valuator.Services;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IShardManager _shardManager;
    private readonly IRabbitMQService _service;
    private static readonly Dictionary<string, string> CountryRegions = new Dictionary<string, string>
    {
            { "Russia", "RU" },
            { "France", "EU" },
            { "Germany", "EU" },
            { "UAE", "ASIA" },
            { "India", "ASIA" }
    };
    public string Port { get; set; }

    public IndexModel(ILogger<IndexModel> logger, IShardManager shardManager, IRabbitMQService rabbitMQService)
    {
        _logger = logger;
        _shardManager = shardManager;
        _service = rabbitMQService;
        Port = Environment.GetEnvironmentVariable("EXTERNAL_PORT") ?? "NO PORT";
    }

    public IActionResult OnPost(string text, string country, CancellationTokenSource cts)
    {
        _logger.LogDebug(text);

        if (!User.Identity!.IsAuthenticated)
        {
            return Page();
        }

        if (string.IsNullOrEmpty(text))
        {
            return Page();
        }

        string id = Guid.NewGuid().ToString();

        string region = CountryRegions[country];

        Console.WriteLine("REGION: " + region);

        _shardManager.SetToMain(id, region);

        _shardManager.SetShard(id);
        bool similarity = _shardManager.IsDuplicateText(text);
        _service.SendSimilarityMessage(similarity, id, cts);
		_shardManager.SetToRegion(id, similarity, "SIMILARITY-");

        _shardManager.SetToRegion(id, text, "TEXT-");

        string username = User.FindFirst(ClaimTypes.Name)!.Value;

        _shardManager.SetToRegion(id, username, "AUTHOR-");

        _service.SendTextMessage(id, cts);
        
        return Redirect($"summary?id={id}");
    }
}
