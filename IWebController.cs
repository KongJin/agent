using System.Threading.Tasks;

namespace WebAgentCli;

public interface IWebController
{
    Task ClickAsync(string cssSelector);
    Task<string> GetDomSummaryAsync();
}
