using System.Threading.Tasks;
using OpenQA.Selenium;

namespace WebAgentCli;

/// <summary>Click interactions.</summary>
public interface IClickController
{
    Task ClickAsync(string cssSelector);
    Task ClickAsync(IWebElement element);
}
