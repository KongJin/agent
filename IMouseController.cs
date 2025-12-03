using System.Threading.Tasks;
using OpenQA.Selenium;

namespace WebAgentCli;

/// <summary>Mouse move/scroll interactions.</summary>
public interface IMouseController
{
    Task MoveMouseAsync(int x, int y);
    Task MoveMouseToElementAsync(IWebElement element);
    Task ScrollAsync(string arguments);
}
