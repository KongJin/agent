using System.Threading.Tasks;
using OpenQA.Selenium;

namespace WebAgentCli;

public class CloseTabTool : IAgentTool
{
    private readonly IWebController _web;
    public string Name => "CloseTab";
    public string Description => "Closes the current browser tab/window. Args: none.";

    public CloseTabTool(IWebController web)
    {
        _web = web;
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        try
        {
            await _web.CloseCurrentTabAsync();
            return "Closed current tab/window.";
        }
        catch (System.Exception ex)
        {
            return $"Failed to close current tab: {ex.Message}";
        }
    }
}
