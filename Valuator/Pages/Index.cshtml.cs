using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IConnectionMultiplexer _redis;

    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
    }

    public IActionResult OnPost(string text, CancellationToken cancellationToken)
    {
        _logger.LogDebug(text);

        var db = _redis.GetDatabase();

        string id = Guid.NewGuid().ToString();

        string textKey = "TEXT-" + id;
        // TODO: (pa1) сохранить в БД (Redis) text по ключу textKey
        db.StringSet(textKey, text);

        string rankKey = "RANK-" + id;
        // TODO: (pa1) посчитать rank и сохранить в БД (Redis) по ключу rankKey
        double rank = CalculateRank(text);
        db.StringSet(rankKey, rank);

        string similarityKey = "SIMILARITY-" + id;
        // TODO: (pa1) посчитать similarity и сохранить в БД (Redis) по ключу similarityKey
        bool similarity = IsDuplicateText(text, id);
        db.StringSet(similarityKey, similarity);

        return Redirect($"summary?id={id}");
    }

    private bool IsDuplicateText(string text, string id)
    {
        var db = _redis.GetDatabase();
        var server = _redis.GetServer(_redis.GetEndPoints().First());

        var keys = server.Keys(pattern: "TEXT-*");
        foreach (var key in keys)
        {
            if (key.ToString() == $"TEXT-{id}")
            {
                continue;
            }
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
        int alphabeticCount = text.Count(c =>
            (char.IsLetter(c) &&
            ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
             (c >= 'А' && c <= 'Я') || (c >= 'а' && c <= 'я'))));

        int nonAlphabeticCount = totalChars - alphabeticCount;

        return (double)nonAlphabeticCount / totalChars;
    }
}
