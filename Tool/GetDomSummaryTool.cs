using System.Threading.Tasks;

namespace WebAgentCli;

public class GetDomSummaryTool : IAgentTool
{
    private readonly IWebController _web;

    public string Name => "GetDomSummary";
    public string Description => "Returns a short summary of the current page DOM.";

    public GetDomSummaryTool(IWebController web)
    {
        _web = web;
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        var summary = await _web.GetDomSummaryAsync();
        return summary;
    }
}
