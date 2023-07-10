using System.Net;
using PuppeteerSharp;
using WebSecurityMechanisms.Api.Providers.Interfaces;
using WebSecurityMechanisms.Models;
using ConsoleMessage = WebSecurityMechanisms.Models.ConsoleMessage;
using Request = WebSecurityMechanisms.Models.Request;
using Response = WebSecurityMechanisms.Models.Response;

namespace WebSecurityMechanisms.Api.Providers;

public class HeadlessBrowserProvider : IHeadlessBrowserProvider
{
    private PuppeteerSharp.Browser? _browser = null;
    private PuppeteerSharp.Page? _page = null;

    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly string _headlessFrontUrl;

    public List<ConsoleMessage>? ConsoleMessages { get; private set; }

    public HeadlessBrowserProvider(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _headlessFrontUrl = configuration["HeadlessFrontUrl"];
        _env = env;
    }

    public async Task InitializeBrowserAsync(string correlationId)
    {
        ConsoleMessages = new List<ConsoleMessage>();

        if (_env.IsDevelopment())
        {
            _browser = (PuppeteerSharp.Browser)await PuppeteerSharp.Puppeteer.LaunchAsync(
                new PuppeteerSharp.LaunchOptions
                {
                    Headless = true,
                    ExecutablePath = "/usr/bin/chromium-browser",
                    Args = new[]
                    {
                        "--proxy-server=localhost:1234"
                    }
                });
        }
        else
        {
            if (_browser == null || !_browser.IsConnected)
            {
                _browser = (PuppeteerSharp.Browser)await PuppeteerSharp.Puppeteer.ConnectAsync(new ConnectOptions()
                {
                    BrowserURL = "http://192.168.99.2:9222"
                });
            }
        }


        _page = (PuppeteerSharp.Page)await _browser.NewPageAsync();

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

    public async Task CloseBrowserAsync()
    {
        if (_page != null && !_page.IsClosed)
        {
            _page.Console -= ConsoleEventHandler;
            await _page.CloseAsync();
        }

        if (_browser != null && !_browser.IsClosed)
            await _browser.CloseAsync();
    }

    private void ConsoleEventHandler(object? sender, PuppeteerSharp.ConsoleEventArgs e)
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