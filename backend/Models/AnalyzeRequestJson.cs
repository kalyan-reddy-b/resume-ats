using System.Text.Json.Serialization;

namespace ResumeAnalyzer.Api.Models;

public class AnalyzeRequestJson
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
