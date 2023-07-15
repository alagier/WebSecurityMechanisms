using ConsoleMessage = WebSecurityMechanisms.Models.ConsoleMessage;

namespace WebSecurityMechanisms.Api.Providers.Interfaces;

public interface IHeadlessBrowserProvider
{
    List<ConsoleMessage>? ConsoleMessages { get; }
    
    Task InitializeBrowserAsync(string correlationId);

    Task GoToAsync(string url);

    Task EvaluateExpressionAsync(string payload);
    
    Task WaitForTimeoutAsync(int milliseconds);
}