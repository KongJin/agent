using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebAgentCli;

public class Agent
{
    private readonly LlmClient _llm;
    private readonly Dictionary<string, IAgentTool> _tools;

    public Agent(LlmClient llm, IEnumerable<IAgentTool> tools)
    {
        _llm = llm;
        _tools = tools.ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);
    }

    public async Task RunAsync(string userGoal)
    {
        var systemPrompt = BuildSystemPrompt();
        var context = "";
        bool hasDomSummary = false;
        string? lastTool = null;

        for (int step = 0; step < 10; step++) // 최대 10스텝만
        {
            var userPrompt = BuildUserPrompt(userGoal, context, hasDomSummary);
            var llmResponse = await _llm.ChatAsync(systemPrompt, userPrompt);

            Console.WriteLine($"[LLM raw] {llmResponse}");

            AgentAction action;
            try
            {
                action = JsonSerializer.Deserialize<AgentAction>(llmResponse)
                          ?? throw new Exception("Failed to deserialize AgentAction.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Agent] JSON 파싱 실패: {ex.Message}");
                break;
            }

            if (action.tool == null)
            {
                Console.WriteLine($"[Agent] 작업 종료: {action.final}");
                break;
            }

            // 같은 툴만 계속 호출하면 루프 방지용으로 끊기
            if (lastTool == action.tool)
            {
                Console.WriteLine($"[Agent] 같은 툴 '{action.tool}'을 반복 호출하고 있어서 중단합니다.");
                break;
            }
            lastTool = action.tool;


            if (!_tools.TryGetValue(action.tool, out var tool))
            {
                Console.WriteLine($"[Agent] 미지원 툴: {action.tool}");
                break;
            }

            var result = await tool.ExecuteAsync(action.args ?? "");
            Console.WriteLine($"[Tool:{tool.Name}] result: {result}");

            if (action.tool.Equals("GetDomSummary", StringComparison.OrdinalIgnoreCase))
            {
                hasDomSummary = true;
            }
            context += $"\n[Tool:{tool.Name}] args={action.args}\nResult={result}\n";
        }
    }

    private string BuildSystemPrompt()
    {
        var toolDescriptions = string.Join("\n", _tools.Values.Select(t => $"- {t.Name}: {t.Description}"));

        return @$"
너는 웹 페이지에서 작업을 수행하는 에이전트이다.
다음과 같은 도구들을 사용할 수 있다:

{toolDescriptions}

항상 'JSON 형식'으로만 응답해야 한다. 어떤 설명 문장도 JSON 밖에 쓰지 마라.

- 도구를 호출할 때:
  {{""tool"":""ToolName"",""args"":""string arguments""}}

- 모든 작업이 끝나서 사람이 볼 수 있는 최종 결과를 줄 때:
  {{""tool"":null,""final"":""최종 결과 요약 문장""}}
";
    }

    private string BuildUserPrompt(string goal, string context, bool hasDomSummary)
    {
        if (!hasDomSummary)
        {
            return $@"
사용자의 목표: {goal}

지금까지의 도구 실행 히스토리:
{context}

아직 GetDomSummary 도구를 사용해서 페이지 정보를 수집하지 않았다.
먼저 GetDomSummary 도구를 한 번 호출해서 화면 정보를 파악하라.

응답 형식(반드시 JSON만 출력):
- 도구를 호출할 때:
  {{""tool"":""GetDomSummary"",""args"":""""}}

아직은 final을 보내지 말고, GetDomSummary를 한 번 호출하라.
";
        }
        else
        {
            return $@"
사용자의 목표: {goal}

지금까지의 도구 실행 히스토리:
{context}

이미 GetDomSummary 도구를 사용하여 현재 페이지 정보를 수집했다.
다시 GetDomSummary를 호출하지 마라.

이제 사용자의 목표를 달성하기 위해 필요한 경우 다른 도구(예: ClickElement)를 사용하거나,
더 이상 도구 호출이 필요 없다면 최종 요약(final)을 작성하라.

응답 형식(반드시 JSON만 출력):
- 도구 호출:
  {{""tool"":""ToolName"",""args"":""string args""}}
- 작업 종료:
  {{""tool"":null,""final"":""사용자가 이해할 수 있는 자연어 요약""}}
";
        }
    }

    // LLM 응답용 DTO
    public class AgentAction
    {
        public string? tool { get; set; }
        public string? args { get; set; }
        public string? final { get; set; }
    }
}
