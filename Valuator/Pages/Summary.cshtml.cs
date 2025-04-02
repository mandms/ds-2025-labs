using Microsoft.AspNetCore.Mvc.RazorPages;
using Valuator.Repositories;

namespace Valuator.Pages;
public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly IValuatorRepository _repository;

    public SummaryModel(ILogger<SummaryModel> logger, IValuatorRepository valuatorRepository)
    {
        _logger = logger;
        _repository = valuatorRepository;
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }
    public bool Loading { get; set; } = false;

    public void OnGet(string id)
    {
        _logger.LogDebug(id);
		var rank = _repository.GetValue($"RANK-{id}");
        var similarity = (int)_repository.GetValue($"SIMILARITY-{id}");
        Similarity = similarity;
        if (rank != StackExchange.Redis.RedisValue.Null)
        {
            Rank = Convert.ToDouble(rank);
            return;
        }
        Loading = true;
    }
}
