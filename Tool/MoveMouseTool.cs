using System.Threading.Tasks;
using OpenQA.Selenium;

namespace WebAgentCli;

public class MoveMouseTool : IAgentTool
{
    private readonly IWebController _web;
    private readonly IWebDriver _driver;

    public string Name => "MoveMouse";
    public string Description => "Moves the mouse cursor to a specific position or element. Args: 'x:100|y:200' for absolute coordinates, or 'selector:...' to move to an element.";

    public MoveMouseTool(IWebController web, IWebDriver driver)
    {
        _web = web;
        _driver = driver;
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        var query = arguments.Trim();
        if (string.IsNullOrWhiteSpace(query))
            return "No arguments provided. Use 'x:100|y:200' or 'selector:cssSelector'.";

        try
        {
            // 절대 좌표로 마우스 이동 (x:100|y:200 형식)
            if (query.Contains("x:") && query.Contains("y:"))
            {
                var parts = query.Split('|');
                int x = 0, y = 0;

                foreach (var part in parts)
                {
                    var trimmed = part.Trim();
                    if (trimmed.StartsWith("x:", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(trimmed.Substring(2), out var xVal))
                            x = xVal;
                    }
                    else if (trimmed.StartsWith("y:", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(trimmed.Substring(2), out var yVal))
                            y = yVal;
                    }
                }

                await _web.MoveMouseAsync(x, y);
                Console.WriteLine($"[MoveMouseTool] Mouse moved to ({x}, {y})");
                return $"Mouse moved to ({x}, {y})";
            }

            // CSS selector로 요소 위로 마우스 이동
            else if (query.StartsWith("selector:", StringComparison.OrdinalIgnoreCase))
            {
                var selector = query.Substring(9).Trim();
                var element = _driver.FindElement(By.CssSelector(selector));
                await _web.MoveMouseToElementAsync(element);
                Console.WriteLine($"[MoveMouseTool] Mouse moved to element: {selector}");
                return $"Mouse moved to element: {selector}";
            }

            return "Invalid arguments. Use 'x:100|y:200' or 'selector:cssSelector'.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MoveMouseTool] ERROR: {ex.Message}");
            return $"Failed to move mouse: {ex.Message}";
        }
    }
}
