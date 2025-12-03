using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace WebAgentCli;

public class SeleniumWebController : IWebController
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    public void TryAcceptAlertIfPresent()
    {
        try
        {
            var alert = _driver.SwitchTo().Alert();
            alert.Accept();
            Console.WriteLine("[SeleniumWebController] Browser alert accepted.");
        }
        catch (NoAlertPresentException)
        {
            // ignore
        }
        catch
        {
            // ignore
        }
    }

    public SeleniumWebController(IWebDriver driver, int timeoutSeconds = 10)
    {
        _driver = driver;
        _wait = new WebDriverWait(new SystemClock(), _driver, TimeSpan.FromSeconds(timeoutSeconds), TimeSpan.FromMilliseconds(500));
    }

    public Task ClickAsync(string cssSelector)
    {
        TryAcceptAlertIfPresent();
        var element = _wait.Until(d => d.FindElement(By.CssSelector(cssSelector)));

        // Capture window handles before click
        var beforeHandles = _driver.WindowHandles.ToList();

        try
        {
            element.Click();
        }
        catch
        {
            // Fallback to JS click if normal click fails
            try
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
            }
            catch { }
        }

        // If a new window/tab opened, switch to it
        try
        {
            var waitNew = new WebDriverWait(new SystemClock(), _driver, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(200));
            waitNew.Until(d => d.WindowHandles.Count > beforeHandles.Count);
            var newHandles = _driver.WindowHandles.Except(beforeHandles).ToList();
            if (newHandles.Count > 0)
            {
                _driver.SwitchTo().Window(newHandles[0]);
                // wait for page load (null-safe)
                waitNew.Until(d =>
                {
                    var state = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState");
                    return state != null && state.ToString() == "complete";
                });
            }
        }
        catch { }

        TryAcceptAlertIfPresent();
        return Task.CompletedTask;
    }

    public Task ClickAsync(IWebElement element)
    {
        TryAcceptAlertIfPresent();
        // capture window handles before click
        var beforeHandles = _driver.WindowHandles.ToList();

        try
        {
            element.Click();
        }
        catch
        {
            try
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
            }
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

        TryAcceptAlertIfPresent();
        return Task.CompletedTask;
    }
    public async Task DragAndDropAsync(string sourceSelector, string targetSelector)
    {
        TryAcceptAlertIfPresent();
        var source = _wait.Until(d => d.FindElement(By.CssSelector(sourceSelector)));
        var target = _wait.Until(d => d.FindElement(By.CssSelector(targetSelector)));

        var actions = new OpenQA.Selenium.Interactions.Actions(_driver);
        actions.DragAndDrop(source, target).Perform();

        await Task.CompletedTask;
    }

    public Task<string> GetDomSummaryAsync()
    {
        TryAcceptAlertIfPresent();
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
                var value = input.GetAttribute("value") ?? "";
                var displayValue = value.Length > 80 ? value.Substring(0, 80) + "..." : value;
                sb.AppendLine($"  [Input] name='{name}' id='{id}' type='{type}' placeholder='{placeholder}' value='{displayValue}'");
            }
            catch { }
        }

        // Clickable elements (summary only)
        sb.AppendLine($"\n[\uD074\uB9AD \uAC00\uB2A5 \uC694\uC18C]");
        
        // Buttons
        var buttons = _driver.FindElements(By.TagName("button"));
        var buttonTexts = buttons.Take(10)
                                 .Select(b => b.Text?.Trim() ?? "")
                                 .Where(t => !string.IsNullOrEmpty(t))
                                 .ToList();
        sb.AppendLine($"[\uBC84\uD2BC \uC218: {buttons.Count}] \uD14D\uC2A4\uD2B8='{string.Join(", ", buttonTexts)}'");

        // Links
        var links = _driver.FindElements(By.TagName("a"));
        var linkTexts = links.Select(a => a.Text?.Trim() ?? "")
                             .Where(t => !string.IsNullOrEmpty(t))
                             .Take(10)
                             .ToList();
        sb.AppendLine($"[\uB9C1\uD06C \uC218: {links.Count}] \uD14D\uC2A4\uD2B8='{string.Join(", ", linkTexts)}'");

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
        TryAcceptAlertIfPresent();
        Console.WriteLine($"[SeleniumWebController.InputTextAsync] selector='{cssSelector}', text='{text}'");
        
        try
        {
            IWebElement? element = null;
            // Try to find element in current document with wait
            try
            {
                element = _wait.Until(d =>
                {
                    try
                    {
                        var e = d.FindElement(By.CssSelector(cssSelector));
                        return (e.Displayed || e.Enabled) ? e : null;
                    }
                    catch
                    {
                        return null;
                    }
                });

                if (element != null)
                    Console.WriteLine($"[SeleniumWebController] Element found in main document: {element.GetAttribute("name")} = {element.GetAttribute("id")}");
            }
            catch
            {
                // ignore - we'll try frames next
            }

            // If not found in main document, try inside iframes (use short waits)
            if (element == null)
            {
                var frames = _driver.FindElements(By.TagName("iframe"));
                foreach (var frame in frames)
                {
                    try
                    {
                        _driver.SwitchTo().Frame(frame);
                        element = _wait.Until(d =>
                        {
                            try
                            {
                                var e = d.FindElement(By.CssSelector(cssSelector));
                                return (e.Displayed || e.Enabled) ? e : null;
                            }
                            catch { return null; }
                        });

                        if (element != null)
                        {
                            Console.WriteLine($"[SeleniumWebController] Element found inside iframe: {element.GetAttribute("name")} = {element.GetAttribute("id")}");
                            // keep driver inside this frame for subsequent interactions
                            break;
                        }
                        else
                        {
                            _driver.SwitchTo().DefaultContent();
                        }
                    }
                    catch
                    {
                        try { _driver.SwitchTo().DefaultContent(); } catch { }
                    }
                }
            }

            if (element == null)
            {
                throw new Exception($"Element not found for selector: {cssSelector}");
            }

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

            // Final fallback: set value via JavaScript and dispatch a richer set of events (React/Vue friendly)
            try
            {
                Console.WriteLine($"[SeleniumWebController] Using JS fallback to input text.");

                var script = @"
                    (function(el, val) {
                        try {
                            el.focus();
                        } catch(e) {}
                        var lastValue = el.value;
                        el.value = val;
                        // react value tracker hack
                        try {
                            var tracker = el._valueTracker || (Object.getOwnPropertyNames(el).find(function(k){return k.indexOf('_valueTracker')>=0})? el._valueTracker : null);
                            if (tracker && tracker.setValue) tracker.setValue(lastValue);
                        } catch(e) {}

                        var evts = ['keydown','keypress','input','keyup','change','blur'];
                        evts.forEach(function(n){
                            try { el.dispatchEvent(new Event(n, {bubbles:true})); } catch(e) {}
                        });

                        try { el.dispatchEvent(new KeyboardEvent('keyup', {bubbles:true})); } catch(e) {}
                        return el.value;
                    })
                ";

                // Try up to 3 times and validate the value was actually set
                var success = false;
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        var result = ((IJavaScriptExecutor)_driver).ExecuteScript(script, element, text);
                        var setVal = result == null ? "" : result.ToString();
                        Console.WriteLine($"[SeleniumWebController] JS set attempt {attempt}, readback='{setVal}'");
                        if (setVal == text)
                        {
                            success = true;
                            break;
                        }
                    }
                    catch (Exception jsEx)
                    {
                        Console.WriteLine($"[SeleniumWebController] JS attempt {attempt} failed: {jsEx.Message}");
                    }

                    System.Threading.Thread.Sleep(150);
                }

                if (!success)
                {
                    // Save diagnostics: screenshot + element outerHTML
                    try
                    {
                        var outDir = Path.Combine(Directory.GetCurrentDirectory(), "ToolOutput");
                        Directory.CreateDirectory(outDir);
                        var id = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0,8);
                        // screenshot
                        try
                        {
                            var screenshot = ((ITakesScreenshot)_driver).GetScreenshot();
                            var pngPath = Path.Combine(outDir, $"input_fail_{id}.png");
                            File.WriteAllBytes(pngPath, screenshot.AsByteArray);
                            Console.WriteLine($"[SeleniumWebController] Saved screenshot: {pngPath}");
                        }
                        catch (Exception scEx)
                        {
                            Console.WriteLine($"[SeleniumWebController] Screenshot failed: {scEx.Message}");
                        }

                        // element outerHTML
                        try
                        {
                            var outer = ((IJavaScriptExecutor)_driver).ExecuteScript("return arguments[0].outerHTML;", element);
                            var html = outer == null ? "" : outer.ToString();
                            var htmlPath = Path.Combine(outDir, $"input_fail_{id}.html");
                            File.WriteAllText(htmlPath, html);
                            Console.WriteLine($"[SeleniumWebController] Saved element HTML: {htmlPath}");
                        }
                        catch (Exception htmlEx)
                        {
                            Console.WriteLine($"[SeleniumWebController] Dump HTML failed: {htmlEx.Message}");
                        }
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"[SeleniumWebController] Diagnostic save failed: {ex2.Message}");
                    }

                    throw new Exception($"Failed to input text into '{cssSelector}' after JS fallback (last readback did not match). Try a different selector or send Enter if appropriate.");
                }

                // if success, ensure we leave main document context to be safe
                try { _driver.SwitchTo().DefaultContent(); } catch { }

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
        TryAcceptAlertIfPresent();
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
        TryAcceptAlertIfPresent();
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
        TryAcceptAlertIfPresent();
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

    public Task ScrollAsync(string arguments)
    {
        TryAcceptAlertIfPresent();
        Console.WriteLine($"[SeleniumWebController.ScrollAsync] args='{arguments}'");
        try
        {
            var q = (arguments ?? "").Trim();

            // selector: scroll element into view
            if (q.StartsWith("selector:", StringComparison.OrdinalIgnoreCase))
            {
                var selector = q.Substring(9).Trim();
                try
                {
                    var el = _driver.FindElement(By.CssSelector(selector));
                    ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block:'center', inline:'center'});", el);
                    return Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SeleniumWebController.ScrollAsync] selector scroll failed: {ex.Message}");
                    throw;
                }
            }

            // by:dx|dy -> scrollBy
            if (q.StartsWith("by:", StringComparison.OrdinalIgnoreCase) || (q.Contains("x:") && q.Contains("y:")))
            {
                int dx = 0, dy = 0;
                // support by:dx|dy or x:100|y:200
                var parts = q.StartsWith("by:", StringComparison.OrdinalIgnoreCase) ? q.Substring(3).Split('|') : q.Split('|');
                foreach (var p in parts)
                {
                    var t = p.Trim();
                    if (t.StartsWith("x:", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(t.Substring(2), out dx);
                    }
                    else if (t.StartsWith("y:", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(t.Substring(2), out dy);
                    }
                    else
                    {
                        // if format is like "0" or "100"
                        int v;
                        if (int.TryParse(t, out v)) dy = v;
                    }
                }

                ((IJavaScriptExecutor)_driver).ExecuteScript($"window.scrollBy({dx}, {dy});");
                return Task.CompletedTask;
            }

            // to:top / to:bottom
            if (q.Equals("to:top", StringComparison.OrdinalIgnoreCase))
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript("window.scrollTo(0,0);");
                return Task.CompletedTask;
            }

            if (q.Equals("to:bottom", StringComparison.OrdinalIgnoreCase))
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript("window.scrollTo(0, document.body.scrollHeight || document.documentElement.scrollHeight);");
                return Task.CompletedTask;
            }

            // fallback: try parse single integer -> scrollBy 0,amount
            if (int.TryParse(q, out var amount))
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript($"window.scrollBy(0, {amount});");
                return Task.CompletedTask;
            }

            // unknown args
            Console.WriteLine("[SeleniumWebController.ScrollAsync] Unknown arguments, no action taken.");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeleniumWebController.ScrollAsync] Error: {ex.Message}");
            throw;
        }
    }

    public Task GoBackAsync()
    {
        TryAcceptAlertIfPresent();
        Console.WriteLine("[SeleniumWebController.GoBackAsync] Navigating back");
        try
        {
            _driver.Navigate().Back();
            // wait for ready state
                try
            {
                var wait = new WebDriverWait(new SystemClock(), _driver, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(200));
                wait.Until(d =>
                {
                    var state = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState");
                    return state != null && state.ToString() == "complete";
                });
            }
            catch { }
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeleniumWebController.GoBackAsync] Error: {ex.Message}");
            throw;
        }
    }

    public Task GoForwardAsync()
    {
        TryAcceptAlertIfPresent();
        Console.WriteLine("[SeleniumWebController.GoForwardAsync] Navigating forward");
        try
        {
            _driver.Navigate().Forward();
            try
            {
                var wait = new WebDriverWait(new SystemClock(), _driver, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(200));
                wait.Until(d =>
                {
                    var state = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState");
                    return state != null && state.ToString() == "complete";
                });
            }
            catch { }
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeleniumWebController.GoForwardAsync] Error: {ex.Message}");
            throw;
        }
    }

    public Task CloseCurrentTabAsync()
    {
        TryAcceptAlertIfPresent();
        Console.WriteLine("[SeleniumWebController.CloseCurrentTabAsync] Closing current tab/window");
        try
        {
            var handles = _driver.WindowHandles.ToList();
            var current = _driver.CurrentWindowHandle;

            // Close current window
            _driver.Close();

            // If other handles exist, switch to the last one (or first available)
            try
            {
                var remaining = _driver.WindowHandles.ToList();
                if (remaining.Count > 0)
                {
                    // prefer the last handle in list
                    var toSwitch = remaining.Last();
                    _driver.SwitchTo().Window(toSwitch);
                }
            }
            catch (Exception swEx)
            {
                Console.WriteLine($"[SeleniumWebController.CloseCurrentTabAsync] Switch after close failed: {swEx.Message}");
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeleniumWebController.CloseCurrentTabAsync] Error: {ex.Message}");
            throw;
        }
    }
}
