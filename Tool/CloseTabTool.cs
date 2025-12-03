using System.Threading.Tasks;
using OpenQA.Selenium;

namespace WebAgentCli;

public class CloseTabTool : IAgentTool
{
    private readonly INavigationController _nav;
    public string Name => "CloseTab";
    public string Description => "Closes the current browser tab/window. Args: none.";

    public CloseTabTool(INavigationController nav)
    {
        _nav = nav;
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        try
        {
            await _nav.CloseCurrentTabAsync();
            return "Closed current tab/window.";
        }
        catch (System.Exception ex)
        {
            return $"Failed to close current tab: {ex.Message}";
        }
    }
}
