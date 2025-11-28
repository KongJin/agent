using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WebAgentCli;

public class LlmClient
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public LlmClient(HttpClient httpClient, string model = "gpt-4.1-mini")
    {
        _httpClient = httpClient;
        _model = model;

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("환경변수 OPENAI_API_KEY가 설정되어 있지 않습니다.");

        _httpClient.BaseAddress = new Uri("https://api.openai.com/");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<string> ChatAsync(string systemPrompt, string userPrompt)
    {
        var request = new ChatCompletionRequest
        {
            model = _model,
            messages = new List<ChatMessage>
            {
                new() { role = "system", content = systemPrompt },
                new() { role = "user", content = userPrompt }
            }
        };

        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync("v1/chat/completions", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson);

        var message = completion?.choices?.FirstOrDefault()?.message?.content;
        if (string.IsNullOrWhiteSpace(message))
            throw new InvalidOperationException("LLM 응답이 비어 있습니다.");

        return message;
    }

    // --- 요청/응답용 DTO ---

    public class ChatCompletionRequest
    {
        public string model { get; set; } = "";
        public List<ChatMessage> messages { get; set; } = new();
    }

    public class ChatMessage
    {
        public string role { get; set; } = "";
        public string content { get; set; } = "";
    }

    public class ChatCompletionResponse
    {
        public List<Choice> choices { get; set; } = new();
    }

    public class Choice
    {
        public ChatMessage message { get; set; } = new();
    }
}
