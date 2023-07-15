using PuppeteerSharp;
using WebSecurityMechanisms.Api.Providers.Interfaces;
using ConsoleMessage = WebSecurityMechanisms.Models.ConsoleMessage;

namespace WebSecurityMechanisms.Api.Providers;

public class HeadlessBrowserProvider : IHeadlessBrowserProvider
{
    private Browser? _browser;
    private Page? _page;

    private readonly IWebHostEnvironment _env;
    private readonly string _headlessBrowserUrl;
    private readonly string _proxyHost;

    public List<ConsoleMessage>? ConsoleMessages { get; private set; }

    public HeadlessBrowserProvider(IConfiguration configuration, IWebHostEnvironment env)
    {
        var configuration1 = configuration;
        _headlessBrowserUrl = configuration1["HeadlessBrowserUrl"] ?? throw new Exception("_headlessBrowserUrl can't be null");
        _proxyHost = configuration1["ProxyHost"] ?? throw new Exception("_proxyHost can't be null");
        _env = env;
    }

    public async Task InitializeBrowserAsync(string correlationId)
    {
        ConsoleMessages = new List<ConsoleMessage>();

        if (_env.IsDevelopment())
        {
            _browser = (Browser)await Puppeteer.LaunchAsync(
                new LaunchOptions
                {
                    Headless = true,
                    ExecutablePath = "/usr/bin/chromium-browser",
                    Args = new[]
                    {
                        $"--proxy-server={_proxyHost}"
                    }
                });
            
            Console.WriteLine($"Headless browser started locally and connected to proxy {_proxyHost}");
        }
        else
        {
            if (_browser == null || !_browser.IsConnected)
            {
                _browser = (Browser)await Puppeteer.ConnectAsync(new ConnectOptions()
                {
                    BrowserURL = _headlessBrowserUrl
                });
                
                Console.WriteLine($"API connected to remote headless browser {_headlessBrowserUrl}");
            }
        }

        _page = (Page)await _browser.NewPageAsync();

        await _page.SetUserAgentAsync(correlationId);

        _page.Console += ConsoleEventHandler;
    }

    public async Task GoToAsync(string url)
    {
        if (_page == null)
            throw new Exception("_page can't be null");

        await _page.GoToAsync(url);
    }

    public async Task EvaluateExpressionAsync(string payload)
    {
        if (_page == null)
            throw new Exception("_page can't be null");

        await _page.EvaluateExpressionAsync(payload);
    }

    public async Task WaitForTimeoutAsync(int milliseconds)
    {
        if (_page == null)
            throw new Exception("_page can't be null");

        await _page.WaitForTimeoutAsync(milliseconds);
    }

    private void ConsoleEventHandler(object? sender, ConsoleEventArgs e)
    {
        if (ConsoleMessages != null)
            ConsoleMessages.Add(new ConsoleMessage()
            {
                Text = e.Message.Text,
                Type = e.Message.Type.ToString()
            });
        else
            throw new Exception("_browserNavigationData can't be null");
    }
}