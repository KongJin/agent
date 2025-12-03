using System;
using OpenQA.Selenium;

namespace WebAgentCli;

/// <summary>
/// Encapsulates alert/permission prompt handling.
/// </summary>
public class AlertHandler : IAlertHandler
{
    private readonly IWebDriver _driver;

    public AlertHandler(IWebDriver driver)
    {
        _driver = driver;
    }

    public bool TryAcceptIfPresent()
    {
        try
        {
            var alert = _driver.SwitchTo().Alert();
            alert.Accept();
            Console.WriteLine("[AlertHandler] Alert accepted.");
            return true;
        }
        catch (NoAlertPresentException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    public bool TryDismissIfPresent()
    {
        try
        {
            var alert = _driver.SwitchTo().Alert();
            alert.Dismiss();
            Console.WriteLine("[AlertHandler] Alert dismissed.");
            return true;
        }
        catch (NoAlertPresentException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }
}
