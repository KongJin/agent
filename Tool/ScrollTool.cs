using System.Threading.Tasks;
using OpenQA.Selenium;

namespace WebAgentCli;

public class ScrollTool : IAgentTool
{
    private readonly IMouseController _mouse;
    private readonly IWebDriver _driver;

    public string Name => "Scroll";
    public string Description => "Scrolls the page. Args: 'by:dx|dy' (e.g. 'by:0|500'), 'x:100|y:200', 'to:top', 'to:bottom', or 'selector:cssSelector'";

    public ScrollTool(IMouseController mouse, IWebDriver driver)
    {
        _mouse = mouse;
        _driver = driver;
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        var args = (arguments ?? "").Trim();
        if (string.IsNullOrEmpty(args))
            return "No arguments provided. Use 'by:0|500', 'x:100|y:200', 'to:bottom', or 'selector:...'.";

        try
        {
            await _mouse.ScrollAsync(args);
            return $"Scrolled with args: {args}";
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"[ScrollTool] ERROR: {ex.Message}");
            return $"Failed to scroll: {ex.Message}";
        }
    }
}
