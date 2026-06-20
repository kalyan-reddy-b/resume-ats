using System.Collections.Concurrent;
using ResumeAnalyzer.Api.Models;

namespace ResumeAnalyzer.Api.Repository;

public class InMemoryAnalysisRepository : IAnalysisRepository
{
    private readonly ConcurrentDictionary<string, ResumeAnalysis> _cache = new();

    public Task<ResumeAnalysis?> GetByHashAsync(string hash)
    {
        _cache.TryGetValue(hash, out var analysis);
        return Task.FromResult(analysis);
    }

    public Task SaveAsync(string hash, ResumeAnalysis analysis)
    {
        _cache[hash] = analysis;
        return Task.CompletedTask;
    }
}
