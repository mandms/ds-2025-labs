using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Valuator.Pages;
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

    public void OnGet(string id)
    {
        _logger.LogDebug(id);

        _shardManager.SetShard(id);

        var rank = _shardManager.GetRank(id);
        var similarity = _shardManager.GetSimilarity(id);

        Console.WriteLine("RANK:" + rank);
        Console.WriteLine("SIM: " + similarity);

        if (similarity != StackExchange.Redis.RedisValue.Null)
        {
            bool.TryParse(similarity.ToString(), out bool similarityResult);
            Similarity = similarityResult;
        }

        if (rank != StackExchange.Redis.RedisValue.Null)
        {
            Rank = Convert.ToDouble(rank);
            return;
        }
        Loading = true;
    }
}
