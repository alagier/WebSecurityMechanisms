using WebSecurityMechanisms.Models;
using WebSecurityMechanisms.Api.Services.Interfaces;
using WebSecurityMechanisms.Api.Providers.Interfaces;
using WebSecurityMechanisms.Api.Repositories.Interfaces;
using ConsoleMessage = WebSecurityMechanisms.Models.ConsoleMessage;
using Endpoint = WebSecurityMechanisms.Models.Endpoint;

namespace WebSecurityMechanisms.Api.Services;

public class CorsService : ICorsService
{
    private readonly IConfiguration _configuration;
    private readonly IHeadlessBrowserProvider _headlessBrowserProvider;
    private readonly IDiagramProvider _diagramProvider;
    private readonly IProxyRepository _proxyRepository;
    private readonly ICorsRepository _corsRepository;
    private readonly string _headlessFrontUrl;
    private readonly List<string> _corsHeaders;
    private readonly List<Preset> _presets;
    private readonly List<Endpoint> _endpoints;

    public CorsService(IConfiguration configuration, IHeadlessBrowserProvider headlessBrowserProvider,
        IProxyRepository proxyRepository, ICorsRepository corsRepository, IDiagramProvider diagramProvider)
    {
        _configuration = configuration;
        _headlessBrowserProvider = headlessBrowserProvider;
        _diagramProvider = diagramProvider;
        _corsRepository = corsRepository;
        _proxyRepository = proxyRepository;
        _headlessFrontUrl = _configuration["HeadlessFrontUrl"] ?? throw new Exception("_headlessFrontUrl can't be null");
        _corsHeaders = _configuration.GetSection("Cors:Headers").Get<List<string>>() ?? throw new Exception("_corsHeaders can't be null");

        _presets = new List<Preset>()
        {
            new("get") { Name = "GET"},
            new("post") { Name = "POST"},
            new("with-authorized-header") { Name = "With authorized header"},
            new("with-custom-authorized-header") { Name = "With custom header"},
            new("with-credentials") { Name = "With credentials"}
        };

        _endpoints = new List<Endpoint>()
        {
            new("/allorigins"),
            new("/restricted"),
            new("/closed"),
        };
    }

    public async Task<CorsBrowserNavigationData> TestConfigurationAsync(string payload)
    {
        if (payload == null)
            throw new ArgumentNullException(nameof(payload));

        var correlationId = Guid.NewGuid().ToString();

        await _headlessBrowserProvider.InitializeBrowserAsync(correlationId);

        await _headlessBrowserProvider.GoToAsync(_headlessFrontUrl);

        await _headlessBrowserProvider.EvaluateExpressionAsync(payload);

        await _headlessBrowserProvider.WaitForTimeoutAsync(3000);

        var proxyData = await _proxyRepository.GetHttpExchangesByCorrelationIdAsync(correlationId);

        if (proxyData == null)
            throw new NullReferenceException(nameof(proxyData));
        
        return ProcessAndBuildData(proxyData, _headlessBrowserProvider.ConsoleMessages);
    }

    public async Task<string> GetPresetAsync(string preset, string endpoint)
    {
        if (preset == null)
            throw new ArgumentNullException(nameof(preset));
        
        if (endpoint == null)
            throw new ArgumentNullException(nameof(endpoint));
        
        var code = await _corsRepository.GetPresetAsync(preset);
        
        return code.Replace("<APIURL>", $"{_configuration["CorsApiUrl"]}{endpoint}");
    }

    public List<Preset> ListPresets()
    {
        return _presets;
    }

    public List<Endpoint> ListEndpoints()
    {
        return _endpoints;
    }

    private CorsBrowserNavigationData ProcessAndBuildData(List<HttpExchange> httpExchanges,
        List<ConsoleMessage>? consoleMessages)
    {
        bool isWithPreflight = false;
        bool isInError = false;

        httpExchanges = httpExchanges.Where(e => e.Request?.Url != null && !e.Request.Url.StartsWith(_headlessFrontUrl)).ToList();

        httpExchanges.ForEach(e =>
        {
            e.Request?.Headers?.ForEach(h =>
            {
                h.IsHighlighted = _corsHeaders.Any(ch =>
                    string.Equals(ch, h.Key, StringComparison.OrdinalIgnoreCase));
            });

            e.Response?.Headers?.ForEach(h =>
            {
                h.IsHighlighted = _corsHeaders.Any(ch =>
                    string.Equals(ch, h.Key, StringComparison.OrdinalIgnoreCase));
            });
        });

        if (httpExchanges.Count == 0)
        {
            isInError = true;
        }
        else
        {
            isWithPreflight = httpExchanges.First().Request?.Method == HttpMethod.Options.Method;    
        }

        return new CorsBrowserNavigationData()
        {
            ConsoleMessages = consoleMessages,
            HttpExchanges = httpExchanges,
            IsWithPreflight = isWithPreflight,
            IsInError = isInError,
            SequenceDiagram = _diagramProvider.BuildSequenceDiagramFromHttpExchanges(httpExchanges, "Browser", "API")
        };
    }
}