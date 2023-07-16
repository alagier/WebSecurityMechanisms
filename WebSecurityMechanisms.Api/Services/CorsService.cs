using System.Runtime.InteropServices.JavaScript;
using PuppeteerSharp;
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
        _headlessFrontUrl =
            _configuration["HeadlessFrontUrl"] ?? throw new Exception("_headlessFrontUrl can't be null");
        _corsHeaders = _configuration.GetSection("Cors:Headers").Get<List<string>>() ??
                       throw new Exception("_corsHeaders can't be null");

        _presets = new List<Preset>()
        {
            new("get") { Name = "GET" },
            new("post") { Name = "POST" },
            new("with-authorized-header") { Name = "With authorized header" },
            new("with-custom-authorized-header") { Name = "With custom header" },
            new("with-credentials") { Name = "With credentials" }
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

        try
        {
            await _headlessBrowserProvider.EvaluateExpressionAsync(payload);
        }
        catch (EvaluationFailedException e)
        {
            return new CorsBrowserNavigationData()
            {
                IsInError = true,
                Error = e.Message
            };
        }

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
        bool isInError = false;
        string? error = null;

        httpExchanges = httpExchanges.Where(e => e.Request?.Url != null && !e.Request.Url.StartsWith(_headlessFrontUrl))
            .ToList();

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
            return new CorsBrowserNavigationData()
            {
                IsInError = true,
                Error = "An error occurred, please try again or report an issue !"
            };
        }

        return new CorsBrowserNavigationData()
        {
            ConsoleMessages = consoleMessages,
            HttpExchanges = httpExchanges,
            Summary = GetCorsSummary(httpExchanges),
            IsInError = isInError,
            Error = error,
            SequenceDiagram = _diagramProvider.BuildSequenceDiagramFromHttpExchanges(httpExchanges, "Browser", "API")
        };
    }

    private CorsSummary GetCorsSummary(List<HttpExchange> httpExchanges)
    {
        CorsSummary result = new CorsSummary();

        result.IsPreflight = httpExchanges.Count >= 1 &&
                             httpExchanges.First().Request?.Method == HttpMethod.Options.Method;

        var request = httpExchanges.First().Request;
        var response = httpExchanges.First().Response;

        if (request != null && request.Headers != null
                            && response != null && response.Headers != null)
        {
            result.Origin.Requested = request.Headers
                .FirstOrDefault(h => string.Equals(h.Key, "Origin", StringComparison.OrdinalIgnoreCase))
                ?.Value;

            result.Method.Requested = request.Headers
                .FirstOrDefault(h =>
                    string.Equals(h.Key, "Access-Control-Request-Method", StringComparison.OrdinalIgnoreCase))
                ?.Value;

            result.Headers.Requested = request.Headers
                .FirstOrDefault(h =>
                    string.Equals(h.Key, "Access-Control-Request-Headers", StringComparison.OrdinalIgnoreCase))
                ?.Value;

            result.Origin.Received = response.Headers
                .FirstOrDefault(h =>
                    string.Equals(h.Key, "Access-Control-Allow-Origin", StringComparison.OrdinalIgnoreCase))
                ?.Value;

            result.Method.Received = response.Headers
                .FirstOrDefault(h =>
                    string.Equals(h.Key, "Access-Control-Allow-Methods", StringComparison.OrdinalIgnoreCase))
                ?.Value;

            result.Headers.Received = response.Headers
                .FirstOrDefault(h =>
                    string.Equals(h.Key, "Access-Control-Allow-Headers", StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (result.Origin.Requested != null && result.Origin.Received != null)
                result.Origin.isValid = string.Equals(result.Origin.Received, result.Origin.Requested,
                    StringComparison.OrdinalIgnoreCase) || result.Origin.Received == "*";
            else
                result.Origin.isValid = false;

            if (result.Method.Requested != null && result.Method.Received != null)
            {
                var allowedMethods = result.Method.Received.Split(',');

                result.Method.isValid = allowedMethods.Any(m =>
                    string.Equals(m, result.Method.Requested, StringComparison.OrdinalIgnoreCase));
            }
            else if (string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(request.Method, "POST", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(request.Method, "HEAD", StringComparison.OrdinalIgnoreCase))
            {
                result.Method.isValid = true;
            }

            if (result.Headers.Requested != null && result.Headers.Received != null)
            {
                var requestedHeaders = result.Headers.Requested.Split(',', StringSplitOptions.TrimEntries);
                var allowedHeaders = result.Headers.Received.Split(',', StringSplitOptions.TrimEntries);

                foreach (var requestedHeader in requestedHeaders)
                {
                    if (allowedHeaders.Any(h => string.Equals(h, requestedHeader, StringComparison.OrdinalIgnoreCase)))
                        result.Headers.isValid = true;
                    else
                    {
                        result.Headers.isValid = false;
                        break;
                    }
                }
            }
            else if (result.Headers.Requested != null && result.Headers.Received == null)
                result.Headers.isValid = false;
            else
                result.Headers.isValid = true;
        }

        return result;
    }
}