using System.Threading.Tasks;

namespace WebAgentCli;

/// <summary>
/// Facade for web interactions. Keeps backward compatibility while finer-grained interfaces
/// (IClickController, IMouseController, INavigationController) enable ISP/SRP.
/// </summary>
public interface IWebController :
    IClickController,
    IMouseController,
    INavigationController
{
    Task<string> GetDomSummaryAsync();
}
