using System.Threading.Tasks;

namespace WebAgentCli;

/// <summary>
/// Facade for web interactions. Keeps backward compatibility while finer-grained interfaces
/// (IClickController) enable ISP/SRP.
/// </summary>
public interface IWebController :
    IClickController
{
    Task<string> GetDomSummaryAsync();
}
