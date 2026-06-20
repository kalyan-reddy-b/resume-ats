using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using ResumeAnalyzer.Api.Config;
using ResumeAnalyzer.Api.Models;

namespace ResumeAnalyzer.Api.Services;

public class ResumeAnalyzerService : IResumeAnalyzerService
{
    private readonly HttpClient _httpClient;
    private readonly GroqSettings _settings;
    private readonly ILogger<ResumeAnalyzerService> _logger;

    private const string SystemPrompt = @"You are an enterprise-grade ATS scoring engine. You output ONLY valid JSON. No text, no explanation, no markdown — just a single JSON object.

SCORING PROTOCOL:
Start at base score 50. Evaluate the resume against these criteria:

ADDITIONS (only award with clear evidence):
- Quantified achievements: ≥3 metrics with real numbers (%, $, users, latency) = +20. Two metrics = +12. One = +5. Zero = +0.
- Tech stack depth: Technologies listed AND demonstrated in projects/jobs = +15. Listed but not demonstrated = +4. Absent = +0.
- Real projects: Projects with measurable outcomes AND links = +10. Outcomes only = +6. Links only = +3. Neither = +0.
- Language quality: No filler phrases (""responsible for"", ""helped with"", ""worked on"", ""involved in"") = +8. Some filler = +2. Mostly filler = +0.
- Structure: All sections present (Contact, Experience, Skills, Education, Projects), consistent formatting = +5. Partial = +2. Poor = +0.
- Credibility: GitHub/portfolio links, verifiable companies, publications = +7. Partial = +3. None = +0.

DEDUCTIONS (automatic):
- Zero quantified achievements = -25
- ≥3 filler phrases detected = -15
- Tech stack missing or irrelevant to role = -15
- Missing critical keywords for role = -12
- Poor structure = -10
- Unexplained employment gap >6 months = -8 each (max -16)
- Keywords stuffed in skills only, not used in context = -5
- No verifiable links = -5

HARD CAPS (applied after calculation, cannot be overridden):
- No quantified achievements at all → score capped at 52
- Only 1-2 achievements → capped at 62
- Filler language dominant → capped at 58
- No relevant tech stack → capped at 55
- Missing most role keywords → capped at 60
- Fresher with no projects and no internships → capped at 45
- Score ≥80 requires: ≥3 achievements + relevant tech + no filler. Otherwise clamp to 79.
- Score ≥90 requires: all above + verifiable links + senior-level impact. Otherwise clamp to 89.

BEHAVIOR:
- Judge ONLY what is written. Never assume skills not listed.
- ""Developed a feature"" = filler. ""Built payment gateway handling 10K transactions/day"" = evidence.
- A skills list alone does not prove depth. Look for usage in job descriptions and projects.
- Be brutally honest in weaknesses and advice. Do not soften language.

OUTPUT FORMAT (strict JSON, no other text):
{
  ""role"": ""specific inferred role"",
  ""level"": ""Fresher | Junior | Mid-level | Senior"",
  ""score"": <integer 0-100 after all caps applied>,
  ""scoreBreakdown"": {
    ""baseScore"": 50,
    ""earned"": <total points added>,
    ""deducted"": <total points removed>,
    ""capsApplied"": [""list each cap that was triggered, or empty array""]
  },
  ""techStack"": [""only technologies evidenced in resume""],
  ""strengths"": [""max 3 strengths with cited evidence from the resume""],
  ""weaknesses"": [""max 4 weaknesses, be specific and blunt""],
  ""missingKeywords"": [""keywords expected for this role but absent""],
  ""suggestions"": [""max 4 actionable improvements specific to THIS resume""],
  ""advice"": ""2-3 sentences of brutally honest career advice.""
}
";

    public ResumeAnalyzerService(
        HttpClient httpClient,
        IOptions<GroqSettings> settings,
        ILogger<ResumeAnalyzerService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ResumeAnalysis> AnalyzeAsync(string resumeText)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            throw new Exception("GROQ_API_KEY environment variable is not configured");
        }

        string trimmedResume = resumeText.Length > 1800
            ? resumeText.Substring(0, 1800) + "\n[Resume trimmed for processing]"
            : resumeText;

        var requestBody = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = $"Analyze this resume and return ONLY JSON:\n\n{trimmedResume}" }
            },
            temperature = 0.1,
            top_p = 0.9,
            max_tokens = 4096
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        request.Content = JsonContent.Create(requestBody);

        _logger.LogInformation("Sending request to Groq API using model {Model}", _settings.Model);
        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Groq API error: {Status} - {Content}", response.StatusCode, errorContent);
            throw new Exception($"Groq API request failed with status {response.StatusCode}: {errorContent}");
        }

        var jsonResponse = await response.Content.ReadFromJsonAsync<JsonDocument>();
        if (jsonResponse == null)
        {
            throw new Exception("No response received from AI model");
        }

        var responseText = jsonResponse.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new Exception("No text content received from AI model response");
        }

        return ParseResponse(responseText);
    }

    private ResumeAnalysis ParseResponse(string responseText)
    {
        string cleaned = Regex.Replace(responseText, @"<think>[\s\S]*?</think>", "", RegexOptions.IgnoreCase).Trim();

        cleaned = cleaned
            .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
            .Replace("```", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        string jsonString = "";
        int braceDepth = 0;
        int jsonStartIdx = -1;

        for (int i = 0; i < cleaned.Length; i++)
        {
            if (cleaned[i] == '{')
            {
                if (braceDepth == 0) jsonStartIdx = i;
                braceDepth++;
            }
            else if (cleaned[i] == '}')
            {
                braceDepth--;
                if (braceDepth == 0 && jsonStartIdx != -1)
                {
                    string candidate = cleaned.Substring(jsonStartIdx, i - jsonStartIdx + 1);
                    if (candidate.Length > jsonString.Length)
                    {
                        jsonString = candidate;
                    }
                    jsonStartIdx = -1;
                }
            }
        }

        if (string.IsNullOrEmpty(jsonString) && jsonStartIdx != -1 && braceDepth > 0)
        {
            string truncated = cleaned.Substring(jsonStartIdx);
            while (braceDepth > 0)
            {
                truncated += "}";
                braceDepth--;
            }
            jsonString = truncated;
        }

        if (string.IsNullOrEmpty(jsonString))
        {
            _logger.LogError("No JSON found in AI response. Raw cleaned text: {Text}", cleaned);
            throw new Exception("No valid JSON found in AI response");
        }

        try
        {
            var rawNode = JsonNode.Parse(jsonString);
            if (rawNode == null)
            {
                throw new Exception("Failed to parse the extracted JSON string.");
            }

            var analysis = new ResumeAnalysis();
            
            analysis.Role = rawNode["role"]?.ToString() ?? "Unknown Role";
            analysis.Level = rawNode["level"]?.ToString() ?? "Unknown";

            int score = 50;
            if (rawNode["scoreBreakdown"]?["finalScore"] != null)
            {
                int.TryParse(rawNode["scoreBreakdown"]?["finalScore"]?.ToString(), out score);
            }
            else if (rawNode["score"] != null)
            {
                int.TryParse(rawNode["score"]?.ToString(), out score);
            }
            analysis.Score = Math.Clamp(score, 0, 100);

            analysis.TechStack = GetStringList(rawNode["techStack"]);
            analysis.Strengths = GetStringList(rawNode["strengths"]);
            analysis.Weaknesses = GetStringList(rawNode["weaknesses"]);

            var missingKeywords = GetStringList(rawNode["missingKeywords"]);
            if (missingKeywords.Count == 0 && rawNode["keywordAnalysis"]?["missingKeywords"] != null)
            {
                missingKeywords = GetStringList(rawNode["keywordAnalysis"]?["missingKeywords"]);
            }
            analysis.MissingKeywords = missingKeywords;

            analysis.Suggestions = GetStringList(rawNode["suggestions"]);
            analysis.Advice = rawNode["advice"]?.ToString() ?? "No advice provided.";

            return analysis;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parse failed. Raw JSON: {Json}", jsonString);
            throw new Exception("Failed to parse AI response as JSON. The response may have been truncated.", ex);
        }
    }

    private List<string> GetStringList(JsonNode? node)
    {
        var list = new List<string>();
        if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                if (item != null)
                {
                    list.Add(item.ToString());
                }
            }
        }
        return list;
    }
}
