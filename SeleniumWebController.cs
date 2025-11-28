using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace WebAgentCli;

public class SeleniumWebController : IWebController
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public SeleniumWebController(IWebDriver driver, int timeoutSeconds = 10)
    {
        _driver = driver;
        _wait = new WebDriverWait(new SystemClock(), _driver, TimeSpan.FromSeconds(timeoutSeconds), TimeSpan.FromMilliseconds(500));
    }

    public Task ClickAsync(string cssSelector)
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector(cssSelector)));
        element.Click();
        return Task.CompletedTask;
    }

    public Task<string> GetDomSummaryAsync()
    {
        // 실제로는 전체 HTML을 다 보내면 너무 길어서,
        // 간단히 <title> + 주요 a/button/input 텍스트만 뽑는 예시.
        var sb = new StringBuilder();

        try
        {
            var title = _driver.Title;
            sb.AppendLine($"[Title] {title}");
        }
        catch
        {
            // ignore
        }

        var buttons = _driver.FindElements(By.TagName("button"));
        foreach (var btn in buttons.Take(10))
        {
            var text = btn.Text;
            var css = btn.GetAttribute("class");
            sb.AppendLine($"[Button] text='{text}' class='{css}'");
        }

        var links = _driver.FindElements(By.TagName("a"));
        foreach (var a in links.Take(10))
        {
            var text = a.Text;
            var href = a.GetAttribute("href");
            sb.AppendLine($"[Link] text='{text}' href='{href}'");
        }

        return Task.FromResult(sb.ToString());
    }
}
