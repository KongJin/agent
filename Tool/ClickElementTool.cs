using System.Threading.Tasks;

namespace WebAgentCli;

public class ClickElementTool : IAgentTool
{
    private readonly IWebController _web;

    public string Name => "ClickElement";
    public string Description => "Clicks an element using a CSS selector. Args: css selector string.";

    public ClickElementTool(IWebController web)
    {
        _web = web;
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        var selector = arguments.Trim();
        if (string.IsNullOrWhiteSpace(selector))
            return "No selector provided.";

        await _web.ClickAsync(selector);
        return $"Clicked element with selector: {selector}";
    }
}
