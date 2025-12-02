using System.Threading.Tasks;

namespace WebAgentCli;

public class NavigateTool : IAgentTool
{
    private readonly IWebController _web;

    public string Name => "Navigate";
    public string Description => "Navigate browser history. Args: 'back' or 'forward'.";

    public NavigateTool(IWebController web)
    {
        _web = web;
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        var arg = (arguments ?? "").Trim().ToLower();
        if (arg == "back")
        {
            await _web.GoBackAsync();
            return "Navigated back";
        }
        else if (arg == "forward")
        {
            await _web.GoForwardAsync();
            return "Navigated forward";
        }
        else
        {
            return "Invalid arguments. Use 'back' or 'forward'.";
        }
    }
}
