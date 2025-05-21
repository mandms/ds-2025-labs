using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Valuator.Pages;

[Authorize]
public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly IShardManager _shardManager;

    public SummaryModel(ILogger<SummaryModel> logger, IShardManager shardManager)
    {
        _logger = logger;
        _shardManager = shardManager;
    }

    public double Rank { get; set; }
    public bool Similarity { get; set; } = false;
    public bool Loading { get; set; } = false;
    public string? AccessDeniedMessage { get; set; }


    public IActionResult OnGet(string id)
    {
        _logger.LogDebug(id);

        _shardManager.SetShard(id);

        string username = User.FindFirst(ClaimTypes.Name)!.Value;

        var author = _shardManager.GetAuthor(id).ToString();

        if (username != author)
        {
            AccessDeniedMessage = "У вас нет доступа к этим данным";
            return Page();
        }

        var rank = _shardManager.GetRank(id);
        var similarity = _shardManager.GetSimilarity(id);

        if (similarity != StackExchange.Redis.RedisValue.Null)
        {
            bool.TryParse(similarity.ToString(), out bool similarityResult);
            Similarity = similarityResult;
        }

        if (rank != StackExchange.Redis.RedisValue.Null)
        {
            Rank = Math.Round(Convert.ToDouble(rank), 3, MidpointRounding.AwayFromZero);
            return Page();
        }

        Loading = true;
        return Page();
    }
}
