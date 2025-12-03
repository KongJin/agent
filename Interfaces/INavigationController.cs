using System.Threading.Tasks;

namespace WebAgentCli;

/// <summary>History/tab navigation.</summary>
public interface INavigationController
{
    Task GoBackAsync();
    Task GoForwardAsync();
    Task CloseCurrentTabAsync();
}
