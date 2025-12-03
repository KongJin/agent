using System.Text;
using System.Threading.Tasks;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace WebAgentCli;

/// <summary>
/// Handles click/navigation \+ DOM summary. Input/mouse logic lives in dedicated controllers.
/// </summary>
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

        var beforeHandles = _driver.WindowHandles.ToList();

        try
        {
            element.Click();
        }
        catch
        {
            try { ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element); }
            catch { }
        }

        try
        {
            var waitNew = new WebDriverWait(new SystemClock(), _driver, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(200));
            waitNew.Until(d => d.WindowHandles.Count > beforeHandles.Count);
            var newHandles = _driver.WindowHandles.Except(beforeHandles).ToList();
            if (newHandles.Count > 0)
            {
                _driver.SwitchTo().Window(newHandles[0]);
                waitNew.Until(d =>
                {
                    var state = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState");
                    return state != null && state.ToString() == "complete";
                });
            }
        }
        catch { }

        return Task.CompletedTask;
    }

    public Task ClickAsync(IWebElement element)
    {
        var beforeHandles = _driver.WindowHandles.ToList();

        try
        {
            element.Click();
        }
        catch
        {
            try { ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element); }
            catch { }
        }

        try
        {
            var waitNew = new WebDriverWait(new SystemClock(), _driver, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(200));
            waitNew.Until(d => d.WindowHandles.Count > beforeHandles.Count);
            var newHandles = _driver.WindowHandles.Except(beforeHandles).ToList();
            if (newHandles.Count > 0)
            {
                _driver.SwitchTo().Window(newHandles[0]);
                waitNew.Until(d =>
                {
                    var state = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState");
                    return state != null && state.ToString() == "complete";
                });
            }
        }
        catch { }

        return Task.CompletedTask;
    }

    public Task<string> GetDomSummaryAsync()
    {
        var sb = new StringBuilder();

        try
        {
            var title = _driver.Title;
            sb.AppendLine($"[Title] {title}");
        }
        catch { }

        var inputs = _driver.FindElements(By.TagName("input"));
        sb.AppendLine($"\n[Inputs found: {inputs.Count}]");
        foreach (var input in inputs.Take(20))
        {
            try
            {
                var name = input.GetAttribute("name") ?? "(no name)";
                var id = input.GetAttribute("id") ?? "(no id)";
                var placeholder = input.GetAttribute("placeholder") ?? "";
                var type = input.GetAttribute("type") ?? "text";
                var value = input.GetAttribute("value") ?? "";
                var displayValue = value.Length > 80 ? value.Substring(0, 80) + "..." : value;
                sb.AppendLine($"  [Input] name='{name}' id='{id}' type='{type}' placeholder='{placeholder}' value='{displayValue}'");
            }
            catch { }
        }

        sb.AppendLine($"\n[클릭 가능 요소]");
        var buttons = _driver.FindElements(By.TagName("button"));
        var buttonTexts = buttons.Take(10).Select(b => b.Text?.Trim() ?? "").Where(t => !string.IsNullOrEmpty(t)).ToList();
        sb.AppendLine($"[버튼 수: {buttons.Count}] 텍스트='{string.Join(", ", buttonTexts)}'");

        var links = _driver.FindElements(By.TagName("a"));
        var linkTexts = links.Select(a => a.Text?.Trim() ?? "").Where(t => !string.IsNullOrEmpty(t)).Take(10).ToList();
        sb.AppendLine($"[링크 수: {links.Count}] 텍스트='{string.Join(", ", linkTexts)}'");

        try
        {
            var loginElements = _driver.FindElements(By.XPath("//*[contains(text(), '로그인')]"));
            if (loginElements.Count > 0)
            {
                sb.AppendLine($"\n[특수: '로그인' 텍스트를 포함하는 요소 (모든 타입)]");
                foreach (var elem in loginElements.Take(10))
                {
                    try
                    {
                        var tagName = elem.TagName;
                        var text = elem.Text?.Trim() ?? "";
                        var id = elem.GetAttribute("id") ?? "";
                        var className = elem.GetAttribute("class") ?? "";
                        var href = elem.GetAttribute("href") ?? "";
                        var onclick = elem.GetAttribute("onclick") ?? "";

                        var attrStr = "";
                        if (!string.IsNullOrEmpty(id)) attrStr += $" id='{id}'";
                        if (!string.IsNullOrEmpty(className)) attrStr += $" class='{className}'";
                        if (!string.IsNullOrEmpty(href)) attrStr += $" href='{href}'";
                        if (!string.IsNullOrEmpty(onclick)) attrStr += $" onclick=yes";

                        sb.AppendLine($"  [{tagName}] text='{text}'{attrStr}");
                    }
                    catch { }
                }
            }
        }
        catch { }

        var images = _driver.FindElements(By.TagName("img"));
        sb.AppendLine($"[Images: {images.Count}]");
        foreach (var img in images.Take(15))
        {
            try
            {
                var alt = img.GetAttribute("alt") ?? "(no alt)";
                var src = img.GetAttribute("src") ?? "";
                var id = img.GetAttribute("id") ?? "";
                var title = img.GetAttribute("title") ?? "";
                sb.AppendLine($"  [Image] alt='{alt}' id='{id}' title='{title}' src='{src.Substring(0, Math.Min(50, src.Length))}'...");
            }
            catch { }
        }

        return Task.FromResult(sb.ToString());
    }


}



