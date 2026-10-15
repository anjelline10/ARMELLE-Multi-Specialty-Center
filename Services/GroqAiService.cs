using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Armelle.Services;

public sealed class GroqAiService
{
    private readonly HttpClient _http = new();
    private readonly IConfiguration _config;

    public GroqAiService(IConfiguration config) => _config = config;

    public async Task<string> AskAsync(string userMessage, string clinicContext = "")
    {
        var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY")
                     ?? _config["Groq:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
            return "ARMELLE AI is not connected yet. Please ask a staff member or choose Talk to Agent.";

        var model = _config["Groq:Model"] ?? "openai/gpt-oss-20b";

        var system = """
You are ARMELLE AI, the public clinic information assistant for ARMELLE Multi-Specialty Center.
Reply naturally in English, Filipino/Tagalog, or Taglish according to the user's language.
Use the supplied ARMELLE clinic context for clinic-specific facts. Never invent doctors, schedules,
prices, branches, HMO coverage, laboratory results, prescriptions, or availability.
You may explain clinic services, navigation, appointments, walk-ins, laboratory preparation,
pharmacy information, prices, HMO/PhilHealth information, and FAQs when supported by context.
Do not diagnose, prescribe, or replace a licensed clinician. For urgent or emergency symptoms,
tell the user to seek immediate professional/emergency care.
If the user asks for a real person, clearly direct them to the Talk to Agent / Live Support option.
Keep answers concise and helpful.
""";

        var payload = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = system + "\n\nCURRENT ARMELLE CONTEXT:\n" + clinicContext },
                new { role = "user", content = userMessage }
            },
            temperature = 0.3,
            max_completion_tokens = 700
        };

        using var req = new HttpRequestMessage(HttpMethod.Post,
            "https://api.groq.com/openai/v1/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
            return $"ARMELLE AI is temporarily unavailable ({(int)res.StatusCode}). You may use Talk to Agent.";

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message")
            .GetProperty("content").GetString()
            ?? "I could not generate a response. Please try again or talk to a staff member.";
    }
}
