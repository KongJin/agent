using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WebAgentCli;

class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // 1. Selenium WebDriver 준비 (한 번만 초기화)
        var chromeOptions = new ChromeOptions();
        chromeOptions.AddArgument("--start-maximized");

        using IWebDriver driver = new ChromeDriver(chromeOptions);

        // 네이버로 처음 이동
        driver.Navigate().GoToUrl("https://naver.com");

        var webController = new SeleniumWebController(driver);

        // 2. 툴 등록
        var tools = new IAgentTool[]
        {
            new GetDomSummaryTool(webController),
            new ClickElementTool(webController, driver),
            new ClickImageTool(webController, driver),
            new InputTextTool(webController, driver),
            new MoveMouseTool(webController, driver)
            //new DragAndDropTool(webController)
        };

        // 3. LLM 클라이언트
        var httpClient = new HttpClient();
        var llmClient = new LlmClient(httpClient);

        // 4. 에이전트 반복 루프: 사용자가 "exit" 또는 "종료"를 입력할 때까지 계속
        var agent = new Agent(llmClient, tools);
        
        // 처음 한 번 페이지 정보를 출력해서 사용자가 선택할 수 있게 함
        Console.WriteLine("\n[초기화] 페이지 정보를 로드합니다...");
        var getDomTool = new GetDomSummaryTool(webController);
        var pageSummary = await getDomTool.ExecuteAsync("");
        Console.WriteLine("\n========== 현재 페이지 정보 ==========");
        Console.WriteLine(pageSummary);
        Console.WriteLine("=====================================\n");
        
        while (true)
        {
            Console.WriteLine("\n================================");
            Console.WriteLine("페이지에서 수행할 작업을 말해주세요");
            Console.WriteLine("예시: '검색어로 검색', '첫번째 항목 클릭', '로고 이미지 클릭'");
            Console.WriteLine("(종료하려면 'exit' 또는 '종료' 입력):");
            
            string userGoal = "";
            while (string.IsNullOrWhiteSpace(userGoal))
            {
                var input = Console.ReadLine();
                userGoal = input ?? "";
                if (string.IsNullOrWhiteSpace(userGoal))
                {
                    Console.WriteLine("입력값이 비어 있습니다. 다시 입력해 주세요:");
                }
            }

            // 종료 신호 확인
            if (userGoal.Equals("exit", StringComparison.OrdinalIgnoreCase) || 
                userGoal.Equals("종료", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("프로그램을 종료합니다.");
                break;
            }

            Console.WriteLine($"[실행중...] 작업: '{userGoal}'");

            // 에이전트에 자연어 작업을 전달 (LLM이 적절한 도구를 선택)
            await agent.RunAsync(userGoal);

            Console.WriteLine("[완료] 작업이 완료되었습니다.");
            
            // 작업 후 페이지 정보 업데이트
            Console.WriteLine("\n[정보 업데이트] 페이지 상태를 갱신합니다...");
            pageSummary = await getDomTool.ExecuteAsync("");
            Console.WriteLine("\n========== 현재 페이지 정보 ==========");
            Console.WriteLine(pageSummary);
            Console.WriteLine("=====================================\n");
        }
        
        Console.WriteLine("브라우저를 종료합니다.");
    }
}
