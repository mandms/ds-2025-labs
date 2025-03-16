using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IConnectionMultiplexer _redis;
    public string Port { get; set; }

    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
    }

    public void OnGet()
    {
        Port = Environment.GetEnvironmentVariable("EXTERNAL_PORT") ?? "NO PORT";
    }

    public IActionResult OnPost(string text)
    {
        _logger.LogDebug(text);

        var db = _redis.GetDatabase();

        string id = Guid.NewGuid().ToString();

        string rankKey = "RANK-" + id;
        double rank = CalculateRank(text);
        db.StringSet(rankKey, rank);

        string similarityKey = "SIMILARITY-" + id;
        bool similarity = IsDuplicateText(text);
        db.StringSet(similarityKey, similarity);

        if (!similarity)
        {
            string textKey = "TEXT-" + id;
            db.StringSet(textKey, text);
        }

        return Redirect($"summary?id={id}");
    }

    private bool IsDuplicateText(string text)
    {
        var db = _redis.GetDatabase();
        var server = _redis.GetServer("redis", 6379);

        var keys = server.Keys(pattern: "TEXT-*");
        foreach (var key in keys)
        {
            var value = db.StringGet(key);
            if (value.ToString() == text)
            {
                return true;
            }
        }

        return false;
    }

    private double CalculateRank(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        int totalChars = text.Length;
        int nonAlphabeticCount = text.Count(c => !char.IsLetter(c));

        return (double)nonAlphabeticCount / totalChars;
    }
}
