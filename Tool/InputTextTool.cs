using System.Threading.Tasks;
using OpenQA.Selenium;

namespace WebAgentCli;

public class InputTextTool : IAgentTool
{
    private readonly IWebController _web;
    private readonly IWebDriver _driver;

    public string Name => "InputText";
    public string Description => "Inputs text into an element. Args: 'fieldName|text' to find field by label/placeholder, or 'selector:...|text' for direct CSS selector.";

    public InputTextTool(IWebController web, IWebDriver driver)
    {
        _web = web;
        _driver = driver;
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            return "No arguments provided. Use 'fieldName|text' or 'selector:cssSelector|text'.";

        // 인자 파싱: fieldName|text|enter=true 형식
        var parts = arguments.Split('|', 3);
        
        if (parts.Length < 2)
            return "Invalid arguments. Use 'fieldName|text'.";

        var fieldQuery = parts[0].Trim();
        var text = parts[1];
        var enterOption = parts.Length > 2 ? parts[2].Trim() : "";
        var sendEnter = enterOption.Equals("enter=true", StringComparison.OrdinalIgnoreCase);

        Console.WriteLine($"[InputTextTool] DEBUG: fieldQuery='{fieldQuery}', text='{text}', sendEnter={sendEnter}");

        try
        {
            IWebElement? element = null;
            string selector = "";

            // CSS selector로 시작하면 직접 selector 사용
            if (fieldQuery.StartsWith("selector:", StringComparison.OrdinalIgnoreCase))
            {
                selector = fieldQuery.Substring(9).Trim();
                element = _driver.FindElement(By.CssSelector(selector));
                Console.WriteLine($"[InputTextTool] Found element by CSS selector: {selector}");
            }
            else
            {
                // 필드 이름으로 자동 검색 (빠른 방식)
                element = FindInputFieldByQuery(_driver, fieldQuery);
                if (element != null)
                {
                    // 찾은 요소의 name이나 id로 selector 생성
                    var name = element.GetAttribute("name");
                    var id = element.GetAttribute("id");
                    
                    if (!string.IsNullOrEmpty(name))
                    {
                        selector = $"input[name='{name}']";
                    }
                    else if (!string.IsNullOrEmpty(id))
                    {
                        selector = $"#{id}";
                    }
                    else
                    {
                        selector = "input"; // fallback (shouldn't happen)
                    }
                    
                    Console.WriteLine($"[InputTextTool] Found element by field name: {fieldQuery}, selector: {selector}");
                }
            }

            if (element == null)
            {
                return $"Input field not found for: '{fieldQuery}'. Try specifying the field more clearly.";
            }

            // 텍스트 입력 (이미 찾은 element를 직접 사용)
            await _web.InputTextAsync(selector, text);
            Console.WriteLine($"[InputTextTool] Text input completed.");
            
            if (sendEnter)
            {
                // Enter 키를 별도로 전송
                System.Threading.Thread.Sleep(200);
                await _web.SendKeyAsync(selector, Keys.Enter);
                Console.WriteLine($"[InputTextTool] Enter key sent.");
                return $"Input '{text}' into field '{fieldQuery}' and sent Enter key.";
            }
            return $"Input '{text}' into field '{fieldQuery}'.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InputTextTool] ERROR: {ex.Message}");
            return $"Failed to input text into '{fieldQuery}': {ex.Message}";
        }
    }

    /// <summary>
    /// 필드 쿼리(이름)를 기반으로 입력 필드를 찾습니다.
    /// placeholder, name, id, aria-label에서 찾습니다.
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

                // 관련 라벨 찾기 (label 요소와 연결된 경우)
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
            Console.WriteLine($"[InputTextTool] Error finding input field: {ex.Message}");
            return null;
        }
    }
}
