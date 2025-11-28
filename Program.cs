using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WebAgentCli;

class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("에이전트에게 시킬 작업을 입력하세요 (예: '이 페이지 구조를 분석해줘'):");
        var goal = Console.ReadLine() ?? "이 페이지 구조를 분석해줘";

        // 1. Selenium WebDriver 준비
        var chromeOptions = new ChromeOptions();
        chromeOptions.AddArgument("--start-maximized");

        using IWebDriver driver = new ChromeDriver(chromeOptions);

        // 테스트용으로 일단 example.com 으로 이동 (원하는 사이트로 바꿔도 됨)
        driver.Navigate().GoToUrl("https://naver.com");

        var webController = new SeleniumWebController(driver);

        // 2. 툴 등록
        var tools = new IAgentTool[]
        {
            new GetDomSummaryTool(webController),
            new ClickElementTool(webController),
        };

        // 3. LLM 클라이언트
        var httpClient = new HttpClient();
        var llmClient = new LlmClient(httpClient);

        // 4. 에이전트 실행
        var agent = new Agent(llmClient, tools);
        await agent.RunAsync(goal);

        Console.WriteLine("작업이 끝났습니다. 아무 키나 누르면 종료합니다.");
        Console.ReadKey();
    }
}
