using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace WebAgentCli;

/// <summary>
/// Handles input/drag & drop interactions.
/// </summary>
public class SeleniumInputController : IInputController
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    public SeleniumInputController(IWebDriver driver, int timeoutSeconds = 10)
    {
        _driver = driver;
        _wait = new WebDriverWait(new SystemClock(), _driver, TimeSpan.FromSeconds(timeoutSeconds), TimeSpan.FromMilliseconds(500));
    }

    public async Task DragAndDropAsync(string sourceSelector, string targetSelector)
    {
        var source = _wait.Until(d => d.FindElement(By.CssSelector(sourceSelector)));
        var target = _wait.Until(d => d.FindElement(By.CssSelector(targetSelector)));

        var actions = new OpenQA.Selenium.Interactions.Actions(_driver);
        actions.DragAndDrop(source, target).Perform();

        await Task.CompletedTask;
    }

    public Task InputTextAsync(string cssSelector, string text)
    {
        Console.WriteLine($"[SeleniumInputController.InputTextAsync] selector='{cssSelector}', text='{text}'");
        
        try
        {
            IWebElement? element = null;
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
                    Console.WriteLine($"[SeleniumInputController] Element found in main document: {element.GetAttribute("name")} = {element.GetAttribute("id")}");
            }
            catch { }

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
                            Console.WriteLine($"[SeleniumInputController] Element found inside iframe: {element.GetAttribute("name")} = {element.GetAttribute("id")}");
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

            if (text == Keys.Enter)
            {
                Console.WriteLine($"[SeleniumInputController] Sending Enter key directly");
                element.SendKeys(Keys.Enter);
                System.Threading.Thread.Sleep(300);
                return Task.CompletedTask;
            }

            try
            {
                if (!element.Displayed || !element.Enabled)
                {
                    Console.WriteLine($"[SeleniumInputController] Element not visible/enabled, waiting...");
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
            catch { }

            try
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block:'center', inline:'center'});", element);
            }
            catch { }

            try { element.Clear(); } catch { }

            try
            {
                element.SendKeys(text);
                System.Threading.Thread.Sleep(100);
                Console.WriteLine($"[SeleniumInputController] SendKeys successful");
                return Task.CompletedTask;
            }
            catch (OpenQA.Selenium.ElementNotInteractableException)
            {
                Console.WriteLine($"[SeleniumInputController] SendKeys failed, trying fallback");
            }

            try
            {
                var actions = new OpenQA.Selenium.Interactions.Actions(_driver);
                actions.MoveToElement(element).Click().SendKeys(text).Perform();
                System.Threading.Thread.Sleep(100);
                Console.WriteLine($"[SeleniumInputController] Actions successful");
                return Task.CompletedTask;
            }
            catch
            {
                Console.WriteLine($"[SeleniumInputController] Actions failed, trying JS fallback");
            }

            try
            {
                Console.WriteLine($"[SeleniumInputController] Using JS fallback to input text.");

                var script = @"
                    (function(el, val) {
                        try {
                            el.focus();
                        } catch(e) {}
                        var lastValue = el.value;
                        el.value = val;
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

                var success = false;
                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        var result = ((IJavaScriptExecutor)_driver).ExecuteScript(script, element, text);
                        var setVal = result == null ? "" : result.ToString();
                        Console.WriteLine($"[SeleniumInputController] JS set attempt {attempt}, readback='{setVal}'");
                        if (setVal == text)
                        {
                            success = true;
                            break;
                        }
                    }
                    catch (Exception jsEx)
                    {
                        Console.WriteLine($"[SeleniumInputController] JS attempt {attempt} failed: {jsEx.Message}");
                    }

                    System.Threading.Thread.Sleep(150);
                }

                if (!success)
                {
                    try
                    {
                        var outDir = Path.Combine(Directory.GetCurrentDirectory(), "ToolOutput");
                        Directory.CreateDirectory(outDir);
                        var id = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N").Substring(0,8);
                        try
                        {
                            var screenshot = ((ITakesScreenshot)_driver).GetScreenshot();
                            var pngPath = Path.Combine(outDir, $"input_fail_{id}.png");
                            File.WriteAllBytes(pngPath, screenshot.AsByteArray);
                            Console.WriteLine($"[SeleniumInputController] Saved screenshot: {pngPath}");
                        }
                        catch (Exception scEx)
                        {
                            Console.WriteLine($"[SeleniumInputController] Screenshot failed: {scEx.Message}");
                        }

                        try
                        {
                            var outer = ((IJavaScriptExecutor)_driver).ExecuteScript("return arguments[0].outerHTML;", element);
                            var html = outer == null ? "" : outer.ToString();
                            var htmlPath = Path.Combine(outDir, $"input_fail_{id}.html");
                            File.WriteAllText(htmlPath, html);
                            Console.WriteLine($"[SeleniumInputController] Saved element HTML: {htmlPath}");
                        }
                        catch (Exception htmlEx)
                        {
                            Console.WriteLine($"[SeleniumInputController] Dump HTML failed: {htmlEx.Message}");
                        }
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"[SeleniumInputController] Diagnostic save failed: {ex2.Message}");
                    }

                    throw new Exception($"Failed to input text into '{cssSelector}' after JS fallback (last readback did not match). Try a different selector or send Enter if appropriate.");
                }

                try { _driver.SwitchTo().DefaultContent(); } catch { }

                Console.WriteLine($"[SeleniumInputController] JS fallback completed.");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SeleniumInputController] JS fallback failed: {ex.Message}");
                throw new Exception($"Failed to input text into '{cssSelector}': {ex.Message}", ex);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeleniumInputController] Error: {ex.Message}");
            throw;
        }
    }

    public Task SendKeyAsync(string cssSelector, string key)
    {
        Console.WriteLine($"[SeleniumInputController.SendKeyAsync] selector='{cssSelector}', key='{key}'");
        
        var element = _wait.Until(d => d.FindElement(By.CssSelector(cssSelector)));
        
        try
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].focus();", element);
            
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
            
            System.Threading.Thread.Sleep(300);
            Console.WriteLine($"[SeleniumInputController.SendKeyAsync] Key sent successfully");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeleniumInputController.SendKeyAsync] Error: {ex.Message}");
            throw new Exception($"Failed to send key to '{cssSelector}': {ex.Message}", ex);
        }
    }
}
