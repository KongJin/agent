using System;
using System.Linq;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace WebAgentCli;

/// <summary>
/// Handles history navigation and tab closing.
/// </summary>
public class SeleniumNavigationController : INavigationController
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public SeleniumNavigationController(IWebDriver driver, int timeoutSeconds = 10)
    {
        _driver = driver;
        _wait = new WebDriverWait(new SystemClock(), _driver, TimeSpan.FromSeconds(timeoutSeconds), TimeSpan.FromMilliseconds(200));
    }

    public Task GoBackAsync()
    {
        Console.WriteLine("[SeleniumNavigationController.GoBackAsync] Navigating back");
        try
        {
            _driver.Navigate().Back();
            WaitForReadyState();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeleniumNavigationController.GoBackAsync] Error: {ex.Message}");
            throw;
        }
    }

    public Task GoForwardAsync()
    {
        Console.WriteLine("[SeleniumNavigationController.GoForwardAsync] Navigating forward");
        try
        {
            _driver.Navigate().Forward();
            WaitForReadyState();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeleniumNavigationController.GoForwardAsync] Error: {ex.Message}");
            throw;
        }
    }

    public Task CloseCurrentTabAsync()
    {
        Console.WriteLine("[SeleniumNavigationController.CloseCurrentTabAsync] Closing current tab/window");
        try
        {
            var handles = _driver.WindowHandles.ToList();
            _driver.Close();

            try
            {
                var remaining = _driver.WindowHandles.ToList();
                if (remaining.Count > 0)
                {
                    var toSwitch = remaining.Last();
                    _driver.SwitchTo().Window(toSwitch);
                }
            }
            catch (Exception swEx)
            {
                Console.WriteLine($"[SeleniumNavigationController.CloseCurrentTabAsync] Switch after close failed: {swEx.Message}");
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SeleniumNavigationController.CloseCurrentTabAsync] Error: {ex.Message}");
            throw;
        }
    }

    private void WaitForReadyState()
    {
        try
        {
            _wait.Until(d =>
            {
                var state = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState");
                return state != null && state.ToString() == "complete";
            });
        }
        catch { }
    }
}
