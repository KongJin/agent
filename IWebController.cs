using System.Threading.Tasks;
using OpenQA.Selenium;

namespace WebAgentCli;

public interface IWebController
{
    Task ClickAsync(string cssSelector);
    Task ClickAsync(IWebElement element); // element 직접 클릭
    Task<string> GetDomSummaryAsync();

    Task DragAndDropAsync(string sourceSelector, string targetSelector);
    Task InputTextAsync(string cssSelector, string text);
    Task SendKeyAsync(string cssSelector, string key); // 특정 키(예: Enter)를 전송
    
    // 커서 움직임
    Task MoveMouseAsync(int x, int y); // 절대 좌표로 마우스 이동
    Task MoveMouseToElementAsync(IWebElement element); // 요소 위로 마우스 이동
}


