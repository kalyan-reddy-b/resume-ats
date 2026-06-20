using ResumeAnalyzer.Api.Models;

namespace ResumeAnalyzer.Api.Services;

public interface IResumeAnalyzerService
{
    Task<ResumeAnalysis> AnalyzeAsync(string resumeText);
}
