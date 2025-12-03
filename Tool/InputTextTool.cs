using System;
using System.Linq;
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

            // direct CSS selector
            if (fieldQuery.StartsWith("selector:", StringComparison.OrdinalIgnoreCase))
            {
                selector = fieldQuery.Substring(9).Trim();
                element = _driver.FindElement(By.CssSelector(selector));
                Console.WriteLine($"[InputTextTool] Found element by CSS selector: {selector}");
            }
            else
            {
                (element, selector) = FindInputFieldByQuery(_driver, fieldQuery);
                if (element != null)
                {
                    Console.WriteLine($"[InputTextTool] Found element by field name: {fieldQuery}, selector: {selector}");
                }
            }

            if (element == null)
            {
                return $"Input field not found for: '{fieldQuery}'. Try specifying the field more clearly.";
            }

            await _web.InputTextAsync(selector, text);
            Console.WriteLine($"[InputTextTool] Text input completed.");

            if (sendEnter)
            {
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
    /// Find an input-like element by query using label-first heuristics, then scoring across id/name/placeholder/aria/labels/nearby text.
    /// </summary>
    private (IWebElement? element, string selector) FindInputFieldByQuery(IWebDriver driver, string query)
    {
        try
        {
            var inputs = driver.FindElements(By.TagName("input"));
            var textareas = driver.FindElements(By.TagName("textarea"));
            var labels = driver.FindElements(By.TagName("label"));
            var allInputs = inputs.Concat(textareas).ToList();

            var queryNorm = (query ?? string.Empty).Trim().ToLowerInvariant();
            IWebElement? best = null;
            string bestSelector = "";
            int bestScore = -1;

        // Step 1: label-first exact/contains
        foreach (var label in labels)
        {
            var labelText = (label.Text ?? "").Trim();
            var labelNorm = labelText.ToLowerInvariant();
            if (string.IsNullOrEmpty(labelNorm)) continue;
            var exactLabel = labelNorm.Equals(queryNorm, StringComparison.OrdinalIgnoreCase);
            if (!exactLabel && !labelNorm.Contains(queryNorm) && !queryNorm.Contains(labelNorm)) continue;

            var forId = label.GetAttribute("for");
            if (!string.IsNullOrEmpty(forId))
            {
                try
                {
                    var target = driver.FindElement(By.Id(forId));
                    return (target, $"#{forId}");
                }
                catch { }
            }

            try
            {
                var candidate = label.FindElements(By.XPath("following::input[1] | following::textarea[1]")).FirstOrDefault();
                if (candidate != null)
                {
                    return (candidate, BuildSelector(candidate));
                }
            }
            catch { }
        }

            int ScoreText(string source, int exactScore = 100, int containsScore = 70)
            {
                if (string.IsNullOrEmpty(source)) return 0;
                var norm = source.Trim().ToLowerInvariant();
                if (norm == queryNorm) return exactScore;
                if (norm.Contains(queryNorm)) return containsScore;
                return 0;
            }

            foreach (var input in allInputs)
            {
                var score = 0;

                var placeholder = input.GetAttribute("placeholder") ?? "";
                score = Math.Max(score, ScoreText(placeholder, 95, 65));

                var name = input.GetAttribute("name") ?? "";
                score = Math.Max(score, ScoreText(name, 95, 65));

                var id = input.GetAttribute("id") ?? "";
                score = Math.Max(score, ScoreText(id, 100, 70));

                var ariaLabel = input.GetAttribute("aria-label") ?? "";
                score = Math.Max(score, ScoreText(ariaLabel, 95, 65));

                // label[for=id]
                var labelFor = input.GetAttribute("id");
                if (!string.IsNullOrEmpty(labelFor))
                {
                    try
                    {
                        var label = driver.FindElement(By.CssSelector($"label[for='{labelFor}']"));
                        var labelText = label.Text ?? "";
                        score = Math.Max(score, ScoreText(labelText, 105, 80));
                    }
                    catch { }
                }

                // immediately previous sibling text
                try
                {
                    var prevSibling = input.FindElements(By.XPath("preceding-sibling::*[1]")).FirstOrDefault();
                    if (prevSibling != null)
                    {
                        var prevText = prevSibling.Text ?? "";
                        score = Math.Max(score, ScoreText(prevText, 90, 70));
                    }
                }
                catch { }

                // nearest previous label (without for)
                try
                {
                    var nearbyLabel = input.FindElements(By.XPath("preceding::label[1]")).FirstOrDefault();
                    if (nearbyLabel != null)
                    {
                        var nearbyText = nearbyLabel.Text ?? "";
                        score = Math.Max(score, ScoreText(nearbyText, 90, 70));
                    }
                }
                catch { }

                // prefer empty fields and textareas slightly to avoid overwriting filled fields and to target long-form inputs
                var currentVal = input.GetAttribute("value") ?? "";
                if (string.Equals(input.TagName, "textarea", StringComparison.OrdinalIgnoreCase))
                {
                    score += 3;
                    currentVal = input.Text ?? currentVal;
                }
                if (string.IsNullOrEmpty(currentVal))
                {
                    score += 5;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = input;
                    bestSelector = BuildSelector(input);
                }
            }

            return (best, bestSelector);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[InputTextTool] Error finding input field: {ex.Message}");
            return (null, "");
        }
    }

    private string BuildSelector(IWebElement element)
    {
        var tag = element.TagName.ToLowerInvariant();
        var id = element.GetAttribute("id") ?? "";
        var name = element.GetAttribute("name") ?? "";
        var placeholder = element.GetAttribute("placeholder") ?? "";

        if (!string.IsNullOrEmpty(id))
            return $"#{id}";
        if (!string.IsNullOrEmpty(name))
            return $"{tag}[name='{name}']";
        if (!string.IsNullOrEmpty(placeholder))
        {
            var shortened = placeholder.Length > 20 ? placeholder.Substring(0, 20) : placeholder;
            return $"{tag}[placeholder*='{shortened}']";
        }
        return tag;
    }
}
