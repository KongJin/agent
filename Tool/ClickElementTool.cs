using System.Threading.Tasks;
using OpenQA.Selenium;

namespace WebAgentCli;

public class ClickElementTool : IAgentTool
{
    private readonly IWebController _web;
    private readonly IWebDriver _driver;

    public string Name => "ClickElement";
    public string Description => "Clicks an element using a CSS selector or by text. Args: 'selector:...' for CSS selector, or just text to find button/link with that text.";

    public ClickElementTool(IWebController web, IWebDriver driver)
    {
        _web = web;
        _driver = driver;
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        var query = arguments.Trim();
        if (string.IsNullOrWhiteSpace(query))
            return "No arguments provided.";

        try
        {
            IWebElement? element = null;

            // CSS selector로 찾기 (selector: 접두어로 시작)
            if (query.StartsWith("selector:", StringComparison.OrdinalIgnoreCase))
            {
                var selector = query.Substring(9).Trim();
                element = _driver.FindElement(By.CssSelector(selector));
                Console.WriteLine($"[ClickElementTool] Found element by CSS selector: {selector}");
            }
            // 텍스트로 찾기 - 통합된 메서드 사용
            else
            {
                // 모든 클릭 가능한 요소에서 텍스트로 찾기 (button, a, span, div 모두 포함)
                element = FindClickableElementByText(_driver, query);
                if (element != null)
                {
                    var tagName = element.TagName.ToLower();
                    var text = element.Text?.Trim() ?? "";
                    Console.WriteLine($"[ClickElementTool] Found {tagName} element with text: {text}");
                }
            }

            if (element == null)
            {
                return $"Element not found with query: '{query}'";
            }

            // 요소가 화면에 보이지 않으면 스크롤
            try
            {
                var jsExecutor = (IJavaScriptExecutor)_driver;
                jsExecutor.ExecuteScript("arguments[0].scrollIntoView(true);", element);
                System.Threading.Thread.Sleep(300);
            }
            catch { }

            // 요소 클릭
            await _web.ClickAsync(element);
            System.Threading.Thread.Sleep(500);
            Console.WriteLine($"[ClickElementTool] Element clicked successfully");
            return $"Clicked element: {query}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ClickElementTool] ERROR: {ex.Message}");
            return $"Failed to click element '{query}': {ex.Message}";
        }
    }

    /// <summary>
    /// XPath에서 특수 문자를 이스케이프합니다.
    /// </summary>
    private string EscapeXPath(string text)
    {
        // 간단한 이스케이프: 따옴표를 처리
        return text.Replace("'", "\\'");
    }

    /// <summary>
    /// 모든 클릭 가능한 요소에서 텍스트로 찾기 (button, a, span, div, label 등)
    /// XPath를 사용해서 텍스트를 포함하는 모든 요소를 찾음
    /// </summary>
    private IWebElement? FindClickableElementByText(IWebDriver driver, string query)
    {
        try
        {
            // 1) 정확한 텍스트 일치 찾기 (모든 요소)
            var xpathExact = $"//*[text()='{EscapeXPath(query)}' or normalize-space(text())='{EscapeXPath(query)}']";
            var exactElements = driver.FindElements(By.XPath(xpathExact));
            if (exactElements.Count > 0)
            {
                return exactElements[0];
            }

            // 2) 부분 텍스트 일치로 XPath 검색 (가장 관대한 검색)
            var xpathPartial = $"//*[contains(text(), '{EscapeXPath(query)}') or contains(., '{EscapeXPath(query)}')]";
            var partialElements = driver.FindElements(By.XPath(xpathPartial));
            if (partialElements.Count > 0)
            {
                // 클릭 가능한 요소를 우선으로 (button, a, input, span, div with onclick 등)
                var clickable = partialElements.FirstOrDefault(el =>
                {
                    var tag = el.TagName.ToLower();
                    var hasClickHandler = !string.IsNullOrEmpty(el.GetAttribute("onclick")) ||
                                        !string.IsNullOrEmpty(el.GetAttribute("ng-click")) ||
                                        !string.IsNullOrEmpty(el.GetAttribute("@click"));
                    return tag == "button" || tag == "a" || tag == "input" || 
                           (hasClickHandler && (tag == "span" || tag == "div" || tag == "p"));
                });

                return clickable ?? partialElements[0];
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ClickElementTool] Error in FindClickableElementByText: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 필드 쿼리를 기반으로 입력 필드를 찾습니다.
    /// </summary>
    private IWebElement? FindInputFieldByQuery(IWebDriver driver, string query)
    {
        try
        {
            var inputs = driver.FindElements(By.TagName("input"));
            var textareas = driver.FindElements(By.TagName("textarea"));
            var allInputs = inputs.Concat(textareas).ToList();

            foreach (var input in allInputs)
            {
                // placeholder 확인
                var placeholder = input.GetAttribute("placeholder") ?? "";
                if (placeholder.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    return input;

                // name 확인
                var name = input.GetAttribute("name") ?? "";
                if (name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    return input;

                // id 확인
                var id = input.GetAttribute("id") ?? "";
                if (id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    return input;

                // aria-label 확인
                var ariaLabel = input.GetAttribute("aria-label") ?? "";
                if (ariaLabel.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    return input;

                // 관련 라벨 찾기
                var labelFor = input.GetAttribute("id");
                if (!string.IsNullOrEmpty(labelFor))
                {
                    try
                    {
                        var label = driver.FindElement(By.CssSelector($"label[for='{labelFor}']"));
                        var labelText = label.Text ?? "";
                        if (labelText.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                            return input;
                    }
                    catch { }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ClickElementTool] Error finding input field: {ex.Message}");
            return null;
        }
    }
}

