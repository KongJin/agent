using System.Threading.Tasks;
using OpenQA.Selenium;

namespace WebAgentCli;

public class ClickImageTool : IAgentTool
{
    private readonly IWebController _web;
    private readonly IWebDriver _driver;

    public string Name => "ClickImage";
    public string Description => "Clicks an image by alt text or ID. Args: 'alt text' or 'id:imageId'";

    public ClickImageTool(IWebController web, IWebDriver driver)
    {
        _web = web;
        _driver = driver;
    }

    public Task<string> ExecuteAsync(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            return Task.FromResult("No arguments provided. Use 'alt text' or 'id:imageId'.");

        var query = arguments.Trim();

        try
        {
            IWebElement? image = null;

            // ID로 검색
            if (query.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
            {
                var id = query.Substring(3).Trim();
                image = _driver.FindElement(By.Id(id));
                Console.WriteLine($"[ClickImageTool] Found image by ID: {id}");
            }
            // alt text로 검색
            else
            {
                var images = _driver.FindElements(By.TagName("img"));
                foreach (var img in images)
                {
                    var alt = img.GetAttribute("alt") ?? "";
                    if (alt.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        image = img;
                        Console.WriteLine($"[ClickImageTool] Found image by alt text: {alt}");
                        break;
                    }
                }
            }

            if (image == null)
            {
                return Task.FromResult($"Image not found with query: '{query}'");
            }

            // 이미지 클릭
            image.Click();
            System.Threading.Thread.Sleep(500);
            Console.WriteLine($"[ClickImageTool] Image clicked successfully");
            return Task.FromResult($"Clicked image: {query}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ClickImageTool] ERROR: {ex.Message}");
            return Task.FromResult($"Failed to click image '{query}': {ex.Message}");
        }
    }
}
