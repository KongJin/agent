using System.Threading.Tasks;

namespace WebAgentCli;

/// <summary>Text input and drag/drop interactions.</summary>
public interface IInputController
{
    Task DragAndDropAsync(string sourceSelector, string targetSelector);
    Task InputTextAsync(string cssSelector, string text);
    Task SendKeyAsync(string cssSelector, string key);
}
