using WebAgentCli;

public class DragAndDropTool : IAgentTool
{
    private readonly IWebController _web;

    public string Name => "DragAndDrop";
    public string Description => "Drags from one CSS selector to another.";

    public DragAndDropTool(IWebController web)
    {
        _web = web;
    }

    public async Task<string> ExecuteAsync(string arguments)
    {
        // JSON or "selector1|selector2" 형태로 파싱
        var parts = arguments.Split('|');
        await _web.DragAndDropAsync(parts[0], parts[1]);
        return $"Dragged {parts[0]} to {parts[1]}";
    }
}
