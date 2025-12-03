using System.Threading.Tasks;

namespace WebAgentCli;

public class NavigateTool : IAgentTool
{
    private readonly INavigationController _nav;

    public string Name => "Navigate";
    public string Description => "Navigate browser history. Args: 'back' or 'forward'.";

    public NavigateTool(INavigationController nav)
    {
        _nav = nav;
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        var arg = (arguments ?? "").Trim().ToLower();
        if (arg == "back")
        {
            await _nav.GoBackAsync();
            return "Navigated back";
        }
        else if (arg == "forward")
        {
            await _nav.GoForwardAsync();
            return "Navigated forward";
        }
        else
        {
            return "Invalid arguments. Use 'back' or 'forward'.";
        }
    }
}
