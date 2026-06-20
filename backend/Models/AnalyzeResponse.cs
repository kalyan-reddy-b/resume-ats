using System.Text.Json.Serialization;

namespace ResumeAnalyzer.Api.Models;

public class AnalyzeResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public ResumeAnalysis? Data { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
