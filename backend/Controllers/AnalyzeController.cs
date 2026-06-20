using Microsoft.AspNetCore.Mvc;
using ResumeAnalyzer.Api.Helpers;
using ResumeAnalyzer.Api.Models;
using ResumeAnalyzer.Api.Repository;
using ResumeAnalyzer.Api.Services;

namespace ResumeAnalyzer.Api.Controllers;

[ApiController]
[Route("api/analyze")]
public class AnalyzeController : ControllerBase
{
    private readonly IResumeAnalyzerService _analyzerService;
    private readonly IAnalysisRepository _repository;
    private readonly ILogger<AnalyzeController> _logger;

    public AnalyzeController(
        IResumeAnalyzerService analyzerService,
        IAnalysisRepository repository,
        ILogger<AnalyzeController> logger)
    {
        _analyzerService = analyzerService;
        _repository = repository;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Analyze([FromBody] AnalyzeRequestJson request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Text))
            {
                return BadRequest(new AnalyzeResponse { Success = false, Error = "No text provided" });
            }

            string hash = HashHelper.ComputeSha256Hash(request.Text);
            
            var cachedAnalysis = await _repository.GetByHashAsync(hash);
            if (cachedAnalysis != null)
            {
                _logger.LogInformation("Returning cached analysis result for hash: {Hash}", hash);
                return Ok(new AnalyzeResponse { Success = true, Data = cachedAnalysis });
            }

            _logger.LogInformation("Performing AI resume analysis");
            var analysis = await _analyzerService.AnalyzeAsync(request.Text);

            await _repository.SaveAsync(hash, analysis);

            return Ok(new AnalyzeResponse { Success = true, Data = analysis });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analysis failed");
            return StatusCode(500, new AnalyzeResponse 
            { 
                Success = false, 
                Error = ex.Message 
            });
        }
    }
}
