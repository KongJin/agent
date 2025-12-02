using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebAgentCli
{
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

            for (int step = 0; step < 10; step++) // limit to 10 steps
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
                    Console.WriteLine($"[Agent] JSON parse failed: {ex.Message}");
                    break;
                }

                if (action.tool == null)
                {
                    Console.WriteLine($"[Agent] Finished: {action.final}");
                    break;
                }

                var currentToolAction = $"{action.tool}||{action.args}";
                if (lastToolAction != null && string.Equals(lastToolAction, currentToolAction, StringComparison.Ordinal))
                {
                    Console.WriteLine($"[Agent] Repeated identical tool call detected: {action.tool} args={action.args}");
                    break;
                }
                lastToolAction = currentToolAction;

                if (!string.IsNullOrEmpty(action.tool))
                {
                    if (!toolCallCounts.ContainsKey(action.tool)) toolCallCounts[action.tool] = 0;
                    toolCallCounts[action.tool]++;
                    if (toolCallCounts[action.tool] > 5)
                    {
                        Console.WriteLine($"[Agent] Tool '{action.tool}' called too many times: {toolCallCounts[action.tool]}");
                        break;
                    }
                }

                if (!_tools.TryGetValue(action.tool, out var tool))
                {
                    Console.WriteLine($"[Agent] Unsupported tool: {action.tool}");
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

            var header = @"You are a web automation agent. Use the available browser-control tools to achieve the user's goals.
Respond with a single JSON object indicating the tool to call and arguments, or return final when done.
";

            var examples = @"Examples (JSON only):
{ ""tool"": ""GetDomSummary"", ""args"": """" }
{ ""tool"": ""InputText"", ""args"": ""selector:input[name='q']|search text|enter=true"" }
{ ""tool"": ""ClickElement"", ""args"": ""selector:#loginBtn"" }
{ ""tool"": null, ""final"": ""Done"" }
";

            return header + toolDescriptions + "\n" + examples + "\nAlways output exactly one JSON object with either {\"tool\":\"ToolName\",\"args\":\"...\"} or {\"tool\":null,\"final\":\"...\"}.";
        }

        private string BuildUserPrompt(string goal, string context, bool hasDomSummary)
        {
            if (!hasDomSummary)
            {
                return $@"
User goal: {goal}

Tool history:
{context}

You must call GetDomSummary first to read the page state. Respond with JSON only, e.g. {{""tool"":""GetDomSummary"",""args"":""""}}";
            }

            return $@"
User goal: {goal}

Tool history:
{context}

You have a page summary. Use other tools as needed or return final when done.
Respond with JSON only.";
        }

        public class AgentAction
        {
            public string? tool { get; set; }
            public string? args { get; set; }
            public string? final { get; set; }
        }
    }
}
