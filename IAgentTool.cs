using System.Threading.Tasks;

namespace WebAgentCli;

public interface IAgentTool
{
    string Name { get; }          // 예: "ClickElement"
    string Description { get; }   // LLM에게 보여줄 설명
    Task<string> ExecuteAsync(string arguments);
}
