namespace WebSecurityMechanisms.Models;

public class BrowserNavigationData
{
    public List<ConsoleMessage>? ConsoleMessages { get; set; } = new List<ConsoleMessage>();

    public List<HttpExchange> HttpExchanges { get; set; } = new List<HttpExchange>();

    public bool IsInError { get; set; }

    public string Error { get; set; }

    public string? SequenceDiagram { get; set; }
}