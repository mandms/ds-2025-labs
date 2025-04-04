using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Valuator.Repositories;
using Valuator.Services;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IValuatorRepository _repository;
    private readonly IRabbitMQService _service;
    public string Port { get; set; }

    public IndexModel(ILogger<IndexModel> logger, IValuatorRepository valuatorRepository, IRabbitMQService rabbitMQService)
    {
        _logger = logger;
        _repository = valuatorRepository;
        _service = rabbitMQService;
    }

    public void OnGet()
    {
        Port = Environment.GetEnvironmentVariable("EXTERNAL_PORT") ?? "NO PORT";
    }

    public IActionResult OnPost(string text, CancellationTokenSource cts)
    {
        _logger.LogDebug(text);


        string id = Guid.NewGuid().ToString();

        string similarityKey = "SIMILARITY-" + id;
        bool similarity = _repository.IsDuplicateText(text);

        _service.SendSimilarityMessage(similarity, id, cts);

		_repository.SetSimilarity(similarityKey, similarity);

        string textKey = "TEXT-" + id;
        _repository.SetText(textKey, text);

        _service.SendTextMessage(id, cts);
        
        return Redirect($"summary?id={id}");
    }
}
