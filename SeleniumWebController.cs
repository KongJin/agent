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

    public Task ClickAsync(IWebElement element)
    {
        element.Click();
        return Task.CompletedTask;
    }
    public async Task DragAndDropAsync(string sourceSelector, string targetSelector)
    {
        var source = _wait.Until(d => d.FindElement(By.CssSelector(sourceSelector)));
        var target = _wait.Until(d => d.FindElement(By.CssSelector(targetSelector)));

        var actions = new OpenQA.Selenium.Interactions.Actions(_driver);
        actions.DragAndDrop(source, target).Perform();

        await Task.CompletedTask;
    }

    public Task<string> GetDomSummaryAsync()
    {
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

        // Input 요소들 출력 (검색창 찾기 위함)
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
                sb.AppendLine($"  [Input] name='{name}' id='{id}' type='{type}' placeholder='{placeholder}'");
            }
            catch { }
        }

        // 클릭 가능한 요소들 (버튼, 링크, 이미지)
        sb.AppendLine($"\n[Clickable Elements]");
        
        // 버튼들
        var buttons = _driver.FindElements(By.TagName("button"));
        sb.AppendLine($"[Buttons: {buttons.Count}]");
        foreach (var btn in buttons.Take(15))
        {
            try
            {
                var text = btn.Text?.Trim() ?? "(empty)";
                var id = btn.GetAttribute("id") ?? "";
                var onclick = btn.GetAttribute("onclick") ?? "";
                var cssClass = btn.GetAttribute("class") ?? "";
                sb.AppendLine($"  [Button] text='{text}' id='{id}' class='{cssClass}'");
            }
            catch { }
        }

        // 링크들
        var links = _driver.FindElements(By.TagName("a"));
        sb.AppendLine($"[Links: {links.Count}]");
        int linkCount = 0;
        foreach (var a in links)
        {
            try
            {
                var text = a.Text?.Trim() ?? "";
                var href = a.GetAttribute("href") ?? "";
                var id = a.GetAttribute("id") ?? "";
                if (!string.IsNullOrEmpty(text) && linkCount < 25)
                {
                    sb.AppendLine($"  [Link] text='{text}' href='{href}' id='{id}'");
                    linkCount++;
                }
            }
            catch { }
        }

        // 숨겨진 '로그인' 텍스트를 가진 모든 요소 찾기
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

        // 이미지들
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

    public Task InputTextAsync(string cssSelector, string text)
    {
        Console.WriteLine($"[SeleniumWebController.InputTextAsync] selector='{cssSelector}', text='{text}'");
        
        try
        {
            var element = _driver.FindElement(By.CssSelector(cssSelector));
            Console.WriteLine($"[SeleniumWebController] Element found: {element.GetAttribute("name")} = {element.GetAttribute("id")}");

            // 특수 키(예: Enter)인 경우 바로 전송
            if (text == Keys.Enter)
            {
                Console.WriteLine($"[SeleniumWebController] Sending Enter key directly");
                element.SendKeys(Keys.Enter);
                System.Threading.Thread.Sleep(300); // Enter 처리 대기
                return Task.CompletedTask;
            }

            // 요소 확인 (이미 로드된 경우 빠르게)
            try
            {
                if (!element.Displayed || !element.Enabled)
                {
                    Console.WriteLine($"[SeleniumWebController] Element not visible/enabled, waiting...");
                    _wait.Until(d =>
                    {
                        try
                        {
                            var e = d.FindElement(By.CssSelector(cssSelector));
                            return e.Displayed && e.Enabled;
                        }
                        catch
                        {
                            return false;
                        }
                    });
                }
            }
            catch
            {
                // 이미 표시되고 활성화된 경우 무시
            }

            // Scroll into view
            try
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block:'center', inline:'center'});", element);
            }
            catch
            {
                // ignore
            }

            // Try normal Clear + SendKeys first
            try
            {
                element.Clear();
            }
            catch
            {
                // ignore
            }

            try
            {
                element.SendKeys(text);
                System.Threading.Thread.Sleep(100); // 입력 완료 후 짧은 지연
                Console.WriteLine($"[SeleniumWebController] SendKeys successful");
                return Task.CompletedTask;
            }
            catch (OpenQA.Selenium.ElementNotInteractableException)
            {
                Console.WriteLine($"[SeleniumWebController] SendKeys failed (ElementNotInteractable), trying fallback");
            }

            // Fallback: use Actions to focus and send keys
            try
            {
                var actions = new OpenQA.Selenium.Interactions.Actions(_driver);
                actions.MoveToElement(element).Click().SendKeys(text).Perform();
                System.Threading.Thread.Sleep(100);
                Console.WriteLine($"[SeleniumWebController] Actions successful");
                return Task.CompletedTask;
            }
            catch
            {
                Console.WriteLine($"[SeleniumWebController] Actions failed, trying JS fallback");
            }

            // Final fallback: set value via JavaScript and dispatch input/change events (React-friendly)
            try
            {
                Console.WriteLine($"[SeleniumWebController] Using JS fallback to input text.");
                var script = @"
                    (function(el, val) {
                        el.focus();
                        var lastValue = el.value;
                        el.value = val;
                        var tracker = el._valueTracker;
                        if (tracker) tracker.setValue(lastValue);
                        el.dispatchEvent(new Event('input', {bubbles:true}));
                        el.dispatchEvent(new Event('change', {bubbles:true}));
                        el.dispatchEvent(new KeyboardEvent('keyup', {bubbles:true}));
                    })
                ";
                ((IJavaScriptExecutor)_driver).ExecuteScript(script, element, text);
                System.Threading.Thread.Sleep(100);
                Console.WriteLine($"[SeleniumWebController] JS fallback completed.");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SeleniumWebController] JS fallback failed: {ex.Message}");
                throw new Exception($"Failed to input text into '{cssSelector}': {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeleniumWebController] Error: {ex.Message}");
            throw;
        }
    }

    public Task SendKeyAsync(string cssSelector, string key)
    {
        Console.WriteLine($"[SeleniumWebController.SendKeyAsync] selector='{cssSelector}', key='{key}'");
        
        var element = _wait.Until(d => d.FindElement(By.CssSelector(cssSelector)));
        
        try
        {
            // 요소에 포커스 설정
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].focus();", element);
            
            // 키 전송
            if (key == Keys.Enter)
            {
                element.SendKeys(Keys.Enter);
            }
            else if (key == Keys.Tab)
            {
                element.SendKeys(Keys.Tab);
            }
            else
            {
                element.SendKeys(key);
            }
            
            System.Threading.Thread.Sleep(300); // 키 처리 대기
            Console.WriteLine($"[SeleniumWebController.SendKeyAsync] Key sent successfully");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeleniumWebController.SendKeyAsync] Error: {ex.Message}");
            throw new Exception($"Failed to send key to '{cssSelector}': {ex.Message}", ex);
        }
    }

    public Task MoveMouseAsync(int x, int y)
    {
        Console.WriteLine($"[SeleniumWebController.MoveMouseAsync] Moving mouse to ({x}, {y})");
        
        try
        {
            var actions = new OpenQA.Selenium.Interactions.Actions(_driver);
            actions.MoveByOffset(x, y).Perform();
            System.Threading.Thread.Sleep(200);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeleniumWebController.MoveMouseAsync] Error: {ex.Message}");
            throw;
        }
    }

    public Task MoveMouseToElementAsync(IWebElement element)
    {
        Console.WriteLine($"[SeleniumWebController.MoveMouseToElementAsync] Moving mouse to element");
        
        try
        {
            var actions = new OpenQA.Selenium.Interactions.Actions(_driver);
            actions.MoveToElement(element).Perform();
            System.Threading.Thread.Sleep(200);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeleniumWebController.MoveMouseToElementAsync] Error: {ex.Message}");
            throw;
        }
    }
}