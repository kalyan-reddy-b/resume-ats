using System.Text.Json.Serialization;

namespace ResumeAnalyzer.Api.Models;

public class ResumeAnalysis
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public string Level { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("techStack")]
    public List<string> TechStack { get; set; } = new();

    [JsonPropertyName("strengths")]
    public List<string> Strengths { get; set; } = new();

    [JsonPropertyName("weaknesses")]
    public List<string> Weaknesses { get; set; } = new();

    [JsonPropertyName("missingKeywords")]
    public List<string> MissingKeywords { get; set; } = new();

    [JsonPropertyName("suggestions")]
    public List<string> Suggestions { get; set; } = new();

    [JsonPropertyName("advice")]
    public string Advice { get; set; } = string.Empty;
}
