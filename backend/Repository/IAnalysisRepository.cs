using ResumeAnalyzer.Api.Models;

namespace ResumeAnalyzer.Api.Repository;

public interface IAnalysisRepository
{
    Task<ResumeAnalysis?> GetByHashAsync(string hash);
    Task SaveAsync(string hash, ResumeAnalysis analysis);
}
