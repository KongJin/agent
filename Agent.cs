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
        string? lastToolAction = null; // remember last "tool||args" to detect identical repeated actions
        var toolCallCounts = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);

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

            // 같은 툴+인수가 연속 호출되는 경우 중단 (무한 반복 방지)
            var currentToolAction = $"{action.tool}||{action.args}";
            if (lastToolAction != null && string.Equals(lastToolAction, currentToolAction, StringComparison.Ordinal))
            {
                Console.WriteLine($"[Agent] 동일한 툴 호출이 연속으로 반복되어 중단합니다: {action.tool} args={action.args}");
                break;
            }
            lastToolAction = currentToolAction;

            // 툴별 호출 횟수 제한 (예: 한 툴당 최대 5회) — 과도한 반복을 막음
            if (!string.IsNullOrEmpty(action.tool))
            {
                if (!toolCallCounts.ContainsKey(action.tool)) toolCallCounts[action.tool] = 0;
                toolCallCounts[action.tool]++;
                if (toolCallCounts[action.tool] > 5)
                {
                    Console.WriteLine($"[Agent] 툴 '{action.tool}'이 너무 많이 호출되어 중단합니다. 호출수={toolCallCounts[action.tool]}");
                    break;
                }
            }


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

        // JSON 예시는 일반 문자열로 만들어서 보간과 혼동을 피함
        string prompt = $@"
너는 웹 페이지에서 작업을 수행하는 에이전트이다.
다음과 같은 도구들을 사용할 수 있다:

{toolDescriptions}

====== 사용 예시 (빠른 속도) ======
사용자: '비밀번호에 4323 입력해줘'
페이지 정보에서 name='pw' 를 찾았으면:
→ InputText 도구 사용: " + @"{""tool"":""InputText"",""args"":""selector:input[name='pw']|4323""}" + @"

사용자: '아이디 입력창 클릭'
페이지 정보에서 id='id' 를 찾았으면:
→ ClickElement 도구 사용: " + @"{""tool"":""ClickElement"",""args"":""selector:#id""}" + @"

====== 성능 팁 ======
1. GetDomSummary 결과에서 Input 요소의 name 또는 id를 확인한다.
2. 'selector:#id' 또는 'selector:input[name='name']' 형식으로 selector를 사용하면 빠르다.
3. 필드명 검색은 느리므로, 가능하면 selector 형식을 사용한다.

====== 중요 규칙 ======
1. 항상 JSON 형식으로만 응답해야 한다. 어떤 설명 문장도 JSON 밖에 쓰지 마라.
2. 도구의 args는 사용자 목표에서 필요한 정보를 추출해서 전달하라.
3. InputText: selector:...|값 형식을 추천 (빠름)
4. ClickElement: selector:... 형식을 추천, XPath 검색으로 숨겨진 요소도 찾음
5. 사용자가 로그인 관련 요청을 하면: 먼저 GetDomSummary로 로그인 링크/버튼 찾기

도구 호출 형식: {{""tool"":""ToolName"",""args"":""string arguments""}}
작업 종료: {{""tool"":null,""final"":""최종 결과 요약 문장""}}
";
        return prompt;
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

이제 사용자의 목표를 달성하기 위해 필요한 경우 다른 도구를 사용하거나,
더 이상 도구 호출이 필요 없다면 최종 요약(final)을 작성하라.

목표 해석 가이드:
- '필드명에 값 입력' → InputText 도구 사용
- '버튼/링크 클릭' → ClickElement 도구 사용
- '이미지 클릭' → ClickImage 도구 사용
- '페이지 정보 보기' → GetDomSummary 도구 사용

응답 형식(반드시 JSON만 출력):
- 도구 호출:
  {{""tool"":""ToolName"",""args"":""필드명|값""}}
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
