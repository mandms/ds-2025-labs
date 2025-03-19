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

    public void OnGet(string id)
    {
        _logger.LogDebug(id);

        // TODO: (pa1) проинициализировать свойства Rank и Similarity значениями из БД (Redis)
        var rank = (double)_repository.GetValue($"RANK-{id}");
        var similarity = (int)_repository.GetValue($"SIMILARITY-{id}");

        Rank = rank;
        Similarity = similarity;
    }
}
