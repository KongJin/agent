using System;
using System.Threading.Tasks;
using OpenQA.Selenium;

namespace WebAgentCli;

/// <summary>
/// Handles mouse movement and scrolling.
/// </summary>
public class SeleniumMouseController : IMouseController
{
    private readonly IWebDriver _driver;

    public SeleniumMouseController(IWebDriver driver)
    {
        _driver = driver;
    }

    public Task MoveMouseAsync(int x, int y)
    {
        Console.WriteLine($"[SeleniumMouseController.MoveMouseAsync] Moving mouse to ({x}, {y})");

        try
        {
            var actions = new OpenQA.Selenium.Interactions.Actions(_driver);
            actions.MoveByOffset(x, y).Perform();
            System.Threading.Thread.Sleep(200);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeleniumMouseController.MoveMouseAsync] Error: {ex.Message}");
            throw;
        }
    }

    public Task MoveMouseToElementAsync(IWebElement element)
    {
        Console.WriteLine($"[SeleniumMouseController.MoveMouseToElementAsync] Moving mouse to element");

        try
        {
            var actions = new OpenQA.Selenium.Interactions.Actions(_driver);
            actions.MoveToElement(element).Perform();
            System.Threading.Thread.Sleep(200);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeleniumMouseController.MoveMouseToElementAsync] Error: {ex.Message}");
            throw;
        }
    }

    public Task ScrollAsync(string arguments)
    {
        Console.WriteLine($"[SeleniumMouseController.ScrollAsync] args='{arguments}'");
        try
        {
            var q = (arguments ?? "").Trim();

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
                    Console.WriteLine($"[SeleniumMouseController.ScrollAsync] selector scroll failed: {ex.Message}");
                    throw;
                }
            }

            if (q.StartsWith("by:", StringComparison.OrdinalIgnoreCase) || (q.Contains("x:") && q.Contains("y:")))
            {
                int dx = 0, dy = 0;
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
                        if (int.TryParse(t, out var v)) dy = v;
                    }
                }

                ((IJavaScriptExecutor)_driver).ExecuteScript($"window.scrollBy({dx}, {dy});");
                return Task.CompletedTask;
            }

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

            if (int.TryParse(q, out var amount))
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript($"window.scrollBy(0, {amount});");
                return Task.CompletedTask;
            }

            Console.WriteLine("[SeleniumMouseController.ScrollAsync] Unknown arguments, no action taken.");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeleniumMouseController.ScrollAsync] Error: {ex.Message}");
            throw;
        }
    }
}
