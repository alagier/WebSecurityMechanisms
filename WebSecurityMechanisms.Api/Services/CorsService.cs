using WebSecurityMechanisms.Models;
using WebSecurityMechanisms.Api.Services.Interfaces;
using System.Linq;
using PuppeteerSharp;
using WebSecurityMechanisms.Api.Providers.Interfaces;
using WebSecurityMechanisms.Api.Repositories.Interfaces;
using ConsoleMessage = WebSecurityMechanisms.Models.ConsoleMessage;
using Endpoint = WebSecurityMechanisms.Models.Endpoint;
using Request = WebSecurityMechanisms.Models.Request;
using Response = WebSecurityMechanisms.Models.Response;

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
        _headlessFrontUrl = configuration["HeadlessFrontUrl"];
        _corsHeaders = _configuration.GetSection("Cors:Headers").Get<List<string>>();

        _presets = new List<Preset>()
        {
            new() { Key = "simple-get", Name = "GET"},
            new() { Key = "simple-post", Name = "POST"},
            new() { Key = "simple-with-authorized-header", Name = "With authorized header"},
            new() { Key = "preflight-put", Name = "PUT"},
            new() { Key = "preflight-with-custom-authorized-header", Name = "With custom header"}
        };

        _endpoints = new List<Endpoint>()
        {
            new() { Path = "/allorigins"},
            new() { Path = "/restricted"},
            new() { Path = "/closed"},
        };
    }

    public async Task<CorsBrowserNavigationData> TestConfigurationAsync(string payload)
    {
        var correlationId = Guid.NewGuid().ToString();

        await _headlessBrowserProvider.InitializeBrowserAsync(correlationId);

        await _headlessBrowserProvider.GoToAsync(_headlessFrontUrl);

        await _headlessBrowserProvider.EvaluateExpressionAsync(payload);

        await _headlessBrowserProvider.WaitForTimeoutAsync(3000);

        var proxyData = await _proxyRepository.GetHttpExchangesByCorrelationIdAsync(correlationId);

        if (proxyData == null)
            throw new NullReferenceException(nameof(proxyData));

        //await _headlessBrowserProvider.CloseBrowserAsync();

        return ProcessAndBuildData(proxyData, _headlessBrowserProvider.ConsoleMessages);
    }

    public async Task<string> GetPresetAsync(string preset, string endpoint)
    {
        var code = await _corsRepository.GetPresetAsync(preset);
        
        code = code.Replace("<APIURL>", $"{_configuration["CorsApiUrl"]}{endpoint}");

        return code;
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
        List<ConsoleMessage> consoleMessages)
    {
        bool isWithPreflight = false;
        bool isInError = false;
        bool isRequestAllowedByCorsConfiguration = false;
        bool isHttpMethodAllowed = false;

        httpExchanges = httpExchanges.Where(e => !e.Request.Url.StartsWith(_headlessFrontUrl)).ToList();

        httpExchanges.ForEach(e =>
        {
            e.Request.Headers.ForEach(h =>
            {
                h.IsHighlighted = _corsHeaders.Any(ch =>
                    string.Equals(ch, h.Key, StringComparison.OrdinalIgnoreCase));
            });

            e.Response.Headers.ForEach(h =>
            {
                h.IsHighlighted = _corsHeaders.Any(ch =>
                    string.Equals(ch, h.Key, StringComparison.OrdinalIgnoreCase));
            });
        });

        if (httpExchanges == null || httpExchanges.Count == 0)
        {
            isInError = true;
        }
        else
        {
            isWithPreflight = httpExchanges.First().Request.Method == HttpMethod.Options.Method;    
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

    // private bool CheckIfHttpMethodAllowedAndEnrichData(List<HttpExchange> httpExchanges)
    // {
    //     bool ret = false;
    //     string firstHttpRequestMethod = httpExchanges.First().Request.Method.Method;
    //
    //     if (httpExchanges.Count == 1)
    //     {
    //         if (string.Equals(firstHttpRequestMethod, "GET", StringComparison.OrdinalIgnoreCase))
    //             return true;
    //         else if (string.Equals(firstHttpRequestMethod, "POST", StringComparison.OrdinalIgnoreCase))
    //             return true;
    //         else if (string.Equals(firstHttpRequestMethod, "HEAD", StringComparison.OrdinalIgnoreCase))
    //             return true;
    //         else
    //         {
    //             if (httpExchanges.First().Response.Headers.Any(h =>
    //                     string.Equals(h.Key, "Access-Control-Allow-Methods", StringComparison.OrdinalIgnoreCase)))
    //             {
    //                 string allowedMethodsHeader = httpExchanges.First().Response.Headers.First().Value;
    //                 var parsedAllowedMethodsHeader = allowedMethodsHeader.Split(',');
    //
    //                 if (parsedAllowedMethodsHeader.Any(h =>
    //                         string.Equals(h, firstHttpRequestMethod, StringComparison.OrdinalIgnoreCase)))
    //                     return true;
    //             }
    //             else
    //                 return false;
    //         }
    //     }
    //
    //     return ret;
    // }
}