using System.Threading.Tasks;
using OpenQA.Selenium;

namespace WebAgentCli;

/// <summary>
/// Facade for web interactions. Keeps backward compatibility while finer-grained interfaces
/// (IClickController, IInputController, IMouseController, INavigationController) enable ISP/SRP.
/// </summary>
public interface IWebController :
    IClickController,
    IInputController,
    IMouseController,
    INavigationController
{
    Task<string> GetDomSummaryAsync();
}


