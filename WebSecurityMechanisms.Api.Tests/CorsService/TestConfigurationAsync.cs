using System.Net;
using Microsoft.Extensions.Configuration;
using Moq;
using PuppeteerSharp;
using WebSecurityMechanisms.Api.Providers.Interfaces;
using WebSecurityMechanisms.Api.Repositories.Interfaces;
using WebSecurityMechanisms.Models;
using ConsoleMessage = WebSecurityMechanisms.Models.ConsoleMessage;
using Request = WebSecurityMechanisms.Models.Request;
using Response = WebSecurityMechanisms.Models.Response;

namespace WebSecurityMechanisms.Api.Tests.CorsService;

public class TestConfigurationAsync
{
    private Mock<IHeadlessBrowserProvider> _headlessBrowserProviderMock;
    private Mock<IProxyRepository> _proxyRepositoryMock;
    private Mock<ICorsRepository> _corsRepositoryMock;
    private Mock<IDiagramProvider> _diagramProviderMock;
    private IConfiguration _configuration;
    private Services.CorsService _corsService;

    [SetUp]
    public void Setup()
    {
        _headlessBrowserProviderMock = new Mock<IHeadlessBrowserProvider>();
        _proxyRepositoryMock = new Mock<IProxyRepository>();
        _corsRepositoryMock = new Mock<ICorsRepository>();
        _diagramProviderMock = new Mock<IDiagramProvider>();

        _configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json", true, true)
            .Build();

        _corsService = new Services.CorsService(_configuration, _headlessBrowserProviderMock.Object,
            _proxyRepositoryMock.Object,
            _corsRepositoryMock.Object, _diagramProviderMock.Object);
    }

    [Test]
    public async Task TestConfigurationAsync_With_SimplePayload()
    {
        string payload = @"const req = new XMLHttpRequest();
            req.addEventListener(""load"", function () {
                    console.log('OK');
                });
                req.open(""GET"", ""http://cors-api-test.dev/allorigins"");
                req.send();";

        List<HttpExchange> httpExchanges = new List<HttpExchange>()
        {
            new()
            {
                Request = new Request()
                {
                    Method = "GET",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Host", Value = "front-test.dev" },
                        new() { IsHighlighted = false, Key = "User-Agent", Value = "UA" },
                    },
                    PostData = null,
                    Url = "http://front-test.dev"
                },
                Response = new Response()
                {
                    Body = "",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Server", Value = "Kestrel" },
                        new()
                        {
                            IsHighlighted = false, Key = "Set-Cookie", Value = "MyCookie=XXX; path=/"
                        }
                    },
                    Status = HttpStatusCode.OK
                }
            },
            new()
            {
                Request = new Request()
                {
                    Method = "GET",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Host", Value = "cors-api-test.dev" },
                        new() { IsHighlighted = false, Key = "User-Agent", Value = "UA" },
                        new() { IsHighlighted = false, Key = "Origin", Value = "http://front-test.dev" },
                    },
                    PostData = null,
                    Url = "http://cors-api-test.dev/allorigins"
                },
                Response = new Response()
                {
                    Body = "open",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Server", Value = "Kestrel" },
                        new()
                        {
                            IsHighlighted = false, Key = "Access-Control-Allow-Origin", Value = "*"
                        }
                    },
                    Status = HttpStatusCode.OK
                }
            }
        };

        List<ConsoleMessage> consoleMessages = new List<ConsoleMessage>()
        {
            new() { Text = "OK", Type = "Log" }
        };

        var sequenceDiagram =
            "sequenceDiagram\nBrowser->>+API:#8194;#8194;#8194;GET http://cors-api-test.dev/allorigins\nNote over Browser,API:Origin: http://front-test.dev\nAPI->>API:Check CORS configuration\nAPI-->>-Browser:200 OK\nNote over Browser,API:Access-Control-Allow-Origin: *\nBrowser->>+Browser:Interpret API result\n";

        _headlessBrowserProviderMock.Setup(provider => provider.InitializeBrowserAsync(It.IsAny<string>()));
        _headlessBrowserProviderMock.Setup(provider => provider.GoToAsync(_configuration["HeadlessFrontUrl"]));
        _headlessBrowserProviderMock.Setup(provider => provider.EvaluateExpressionAsync(payload));
        _headlessBrowserProviderMock.Setup(provider => provider.WaitForTimeoutAsync(3000));
        _headlessBrowserProviderMock.Setup(provider => provider.ConsoleMessages).Returns(consoleMessages);
        _diagramProviderMock
            .Setup(provider =>
                provider.BuildSequenceDiagramFromHttpExchanges(It.IsAny<List<HttpExchange>>(), "Browser", "API"))
            .Returns(sequenceDiagram);

        _proxyRepositoryMock.Setup(repo => repo.GetHttpExchangesByCorrelationIdAsync(It.IsAny<string>()))
            .ReturnsAsync(httpExchanges);

        var result = await _corsService.TestConfigurationAsync(payload);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ConsoleMessages, Is.Not.Null);
        Assert.That(result.HttpExchanges, Is.Not.Null);
        Assert.That(result.SequenceDiagram, Is.Not.Null);
        Assert.That(result.IsInError, Is.EqualTo(false));

        Assert.That(result.ConsoleMessages.Count, Is.EqualTo(1));
        Assert.That(result.ConsoleMessages.First().Text, Is.EqualTo("OK"));
        Assert.That(result.ConsoleMessages.First().Type, Is.EqualTo("Log"));

        Assert.That(result.HttpExchanges.Count, Is.EqualTo(1));
        var httpExchange = result.HttpExchanges.First();

        Assert.That(httpExchange.Request, Is.Not.Null);
        Assert.That(httpExchange.Request.Method, Is.EqualTo("GET"));
        Assert.That(httpExchange.Request.Url, Is.EqualTo("http://cors-api-test.dev/allorigins"));
        Assert.That(httpExchange.Request.PostData, Is.Null);
        Assert.That(httpExchange.Request.Headers, Is.Not.Null);
        Assert.That(httpExchange.Request.Headers.Count, Is.EqualTo(3));

        var hostHeader =
            httpExchange.Request.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Host", StringComparison.OrdinalIgnoreCase));
        Assert.That(hostHeader, Is.Not.Null);
        Assert.That(hostHeader.IsHighlighted, Is.EqualTo(false));
        Assert.That(hostHeader.Value, Is.EqualTo("cors-api-test.dev"));

        var userAgentHeader =
            httpExchange.Request.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "User-Agent", StringComparison.OrdinalIgnoreCase));
        Assert.That(userAgentHeader, Is.Not.Null);
        Assert.That(userAgentHeader.IsHighlighted, Is.EqualTo(false));
        Assert.That(userAgentHeader.Value, Is.EqualTo("UA"));

        var originHeader =
            httpExchange.Request.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Origin", StringComparison.OrdinalIgnoreCase));
        Assert.That(originHeader, Is.Not.Null);
        Assert.That(originHeader.IsHighlighted, Is.EqualTo(true));
        Assert.That(originHeader.Value, Is.EqualTo("http://front-test.dev"));

        Assert.That(httpExchange.Response, Is.Not.Null);
        Assert.That(httpExchange.Response.Status, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(httpExchange.Response.Body, Is.EqualTo("open"));
        Assert.That(httpExchange.Response.Headers, Is.Not.Null);
        Assert.That(httpExchange.Response.Headers.Count, Is.EqualTo(2));

        var serverHeader =
            httpExchange.Response.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Server", StringComparison.OrdinalIgnoreCase));
        Assert.That(serverHeader, Is.Not.Null);
        Assert.That(serverHeader.IsHighlighted, Is.EqualTo(false));
        Assert.That(serverHeader.Value, Is.EqualTo("Kestrel"));

        var accessAllowOriginHeader =
            httpExchange.Response.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Access-Control-Allow-Origin", StringComparison.OrdinalIgnoreCase));
        Assert.That(accessAllowOriginHeader, Is.Not.Null);
        Assert.That(accessAllowOriginHeader.IsHighlighted, Is.EqualTo(true));
        Assert.That(accessAllowOriginHeader.Value, Is.EqualTo("*"));

        Assert.That(result.SequenceDiagram, Is.EqualTo(sequenceDiagram));
        
        Assert.That(result.Summary, Is.Not.Null);
        Assert.That(result.Summary.IsPreflight, Is.EqualTo(false));
        
        Assert.That(result.Summary.Origin, Is.Not.Null);
        Assert.That(result.Summary.Origin.Name, Is.EqualTo("Origin"));
        Assert.That(result.Summary.Origin.Requested, Is.EqualTo("http://front-test.dev"));
        Assert.That(result.Summary.Origin.Received, Is.EqualTo("*"));
        Assert.That(result.Summary.Origin.isValid, Is.EqualTo(true));
        
        Assert.That(result.Summary.Method, Is.Not.Null);
        Assert.That(result.Summary.Method.Name, Is.EqualTo("Method"));
        Assert.That(result.Summary.Method.Requested, Is.Null);
        Assert.That(result.Summary.Method.Received, Is.Null);
        Assert.That(result.Summary.Method.isValid, Is.EqualTo(true));
        
        Assert.That(result.Summary.Headers, Is.Not.Null);
        Assert.That(result.Summary.Headers.Name, Is.EqualTo("Headers"));
        Assert.That(result.Summary.Headers.Requested, Is.Null);
        Assert.That(result.Summary.Headers.Received, Is.Null);
        Assert.That(result.Summary.Headers.isValid, Is.EqualTo(true));
    }

    [Test]
    public async Task TestConfigurationAsync_With_SimplePayload_And_HeadlessBrowserException()
    {
        string payload = @"const req = new XMLHttpRequest();
            req.addEventListener(""load"", function () {
                    console.log('OK');
                });
                req.open(""GET"", ""http://cors-api-test.dev/allorigins"");
                req.send();";

        List<HttpExchange> httpExchanges = new List<HttpExchange>();

        List<ConsoleMessage> consoleMessages = new List<ConsoleMessage>();

        string sequenceDiagram = null;
        
        _headlessBrowserProviderMock.Setup(provider => provider.InitializeBrowserAsync(It.IsAny<string>()));
        _headlessBrowserProviderMock.Setup(provider => provider.GoToAsync(_configuration["HeadlessFrontUrl"]));
        _headlessBrowserProviderMock.Setup(provider => provider.EvaluateExpressionAsync(payload));
        _headlessBrowserProviderMock.Setup(provider => provider.WaitForTimeoutAsync(3000));
        _headlessBrowserProviderMock.Setup(provider => provider.ConsoleMessages).Returns(consoleMessages);
        _diagramProviderMock
            .Setup(provider =>
                provider.BuildSequenceDiagramFromHttpExchanges(It.IsAny<List<HttpExchange>>(), "Browser", "API"))
            .Returns(sequenceDiagram);

        _proxyRepositoryMock.Setup(repo => repo.GetHttpExchangesByCorrelationIdAsync(It.IsAny<string>()))
            .ReturnsAsync(httpExchanges);

        var result = await _corsService.TestConfigurationAsync(payload);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ConsoleMessages, Is.EqualTo(consoleMessages));
        Assert.That(result.HttpExchanges, Is.EqualTo(httpExchanges));
        Assert.That(result.SequenceDiagram, Is.Null);
        Assert.That(result.IsInError, Is.EqualTo(true));
        Assert.That(result.Error, Is.EqualTo("An error occurred, please try again or report an issue !"));
        Assert.That(result.Summary, Is.Null);
    }
    
    [Test]
    public async Task TestConfigurationAsync_With_SimplePayload_And_HeadlessBrowserError()
    {
        string payload = @"const req = new XMLHttpRequest();
            req.addEventListener(""load"", function () {
                    console.log('OK');
                });
                req.open(""GET"", ""http://cors-api-test.dev/allorigins"");
                req.send();";

        List<HttpExchange> httpExchanges = new List<HttpExchange>();

        List<ConsoleMessage> consoleMessages = new List<ConsoleMessage>();

        string sequenceDiagram = null;
        
        _headlessBrowserProviderMock.Setup(provider => provider.InitializeBrowserAsync(It.IsAny<string>()));
        _headlessBrowserProviderMock.Setup(provider => provider.GoToAsync(_configuration["HeadlessFrontUrl"]));
        _headlessBrowserProviderMock.Setup(provider => provider.EvaluateExpressionAsync(payload)).Throws<EvaluationFailedException>();
        _headlessBrowserProviderMock.Setup(provider => provider.WaitForTimeoutAsync(3000));
        _headlessBrowserProviderMock.Setup(provider => provider.ConsoleMessages).Returns(consoleMessages);
        _diagramProviderMock
            .Setup(provider =>
                provider.BuildSequenceDiagramFromHttpExchanges(It.IsAny<List<HttpExchange>>(), "Browser", "API"))
            .Returns(sequenceDiagram);

        _proxyRepositoryMock.Setup(repo => repo.GetHttpExchangesByCorrelationIdAsync(It.IsAny<string>()))
            .ReturnsAsync(httpExchanges);

        var result = await _corsService.TestConfigurationAsync(payload);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ConsoleMessages, Is.EqualTo(consoleMessages));
        Assert.That(result.HttpExchanges, Is.EqualTo(httpExchanges));
        Assert.That(result.SequenceDiagram, Is.Null);
        Assert.That(result.IsInError, Is.EqualTo(true));
        Assert.That(result.Error, Is.Not.Empty);
        Assert.That(result.Summary, Is.Null);
    }
    
    [Test]
    public async Task TestConfigurationAsync_With_SimplePayload_And_ForbiddenOrigin()
    {
        string payload = @"const req = new XMLHttpRequest();
            req.addEventListener(""load"", function () {
                    console.log('OK');
                });
                req.open(""GET"", ""http://cors-api-test.dev/restricted"");
                req.send();";

        List<HttpExchange> httpExchanges = new List<HttpExchange>()
        {
            new()
            {
                Request = new Request()
                {
                    Method = "GET",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Host", Value = "front-test.dev" },
                        new() { IsHighlighted = false, Key = "User-Agent", Value = "UA" },
                    },
                    PostData = null,
                    Url = "http://front-test.dev"
                },
                Response = new Response()
                {
                    Body = "",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Server", Value = "Kestrel" },
                        new()
                        {
                            IsHighlighted = false, Key = "Set-Cookie", Value = "MyCookie=XXX; path=/"
                        }
                    },
                    Status = HttpStatusCode.OK
                }
            },
            new()
            {
                Request = new Request()
                {
                    Method = "GET",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Host", Value = "cors-api-test.dev" },
                        new() { IsHighlighted = false, Key = "User-Agent", Value = "UA" },
                        new() { IsHighlighted = false, Key = "Origin", Value = "http://front-test.dev" },
                    },
                    PostData = null,
                    Url = "http://cors-api-test.dev/allorigins"
                },
                Response = new Response()
                {
                    Body = "open",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Server", Value = "Kestrel" },
                    },
                    Status = HttpStatusCode.OK
                }
            }
        };

        List<ConsoleMessage> consoleMessages = new List<ConsoleMessage>()
        {
            new() { Text = "Failed", Type = "Error" },
            new() { Text = "Failed to load resource: net::ERR_FAILED", Type = "Error" }
        };

        var sequenceDiagram =
            "sequenceDiagram\nBrowser->>+API:#8194;#8194;#8194;GET http://cors-api-test.dev/allorigins\nNote over Browser,API:Origin: http://front-test.dev\nAPI->>API:Check CORS configuration\nAPI-->>-Browser:200 OK\nNote over Browser,API:Access-Control-Allow-Origin: *\nBrowser->>+Browser:Interpret API result\n";

        _headlessBrowserProviderMock.Setup(provider => provider.InitializeBrowserAsync(It.IsAny<string>()));
        _headlessBrowserProviderMock.Setup(provider => provider.GoToAsync(_configuration["HeadlessFrontUrl"]));
        _headlessBrowserProviderMock.Setup(provider => provider.EvaluateExpressionAsync(payload));
        _headlessBrowserProviderMock.Setup(provider => provider.WaitForTimeoutAsync(3000));
        _headlessBrowserProviderMock.Setup(provider => provider.ConsoleMessages).Returns(consoleMessages);
        _diagramProviderMock
            .Setup(provider =>
                provider.BuildSequenceDiagramFromHttpExchanges(It.IsAny<List<HttpExchange>>(), "Browser", "API"))
            .Returns(sequenceDiagram);

        _proxyRepositoryMock.Setup(repo => repo.GetHttpExchangesByCorrelationIdAsync(It.IsAny<string>()))
            .ReturnsAsync(httpExchanges);

        var result = await _corsService.TestConfigurationAsync(payload);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ConsoleMessages, Is.Not.Null);
        Assert.That(result.HttpExchanges, Is.Not.Null);
        Assert.That(result.SequenceDiagram, Is.Not.Null);
        Assert.That(result.IsInError, Is.EqualTo(false));

        Assert.That(result.ConsoleMessages.Count, Is.EqualTo(2));
        Assert.That(result.ConsoleMessages.First().Text, Is.EqualTo("Failed"));
        Assert.That(result.ConsoleMessages.First().Type, Is.EqualTo("Error"));
        Assert.That(result.ConsoleMessages.Last().Text, Is.EqualTo("Failed to load resource: net::ERR_FAILED"));
        Assert.That(result.ConsoleMessages.Last().Type, Is.EqualTo("Error"));

        Assert.That(result.HttpExchanges.Count, Is.EqualTo(1));
        var httpExchange = result.HttpExchanges.First();

        Assert.That(httpExchange.Request, Is.Not.Null);
        Assert.That(httpExchange.Request.Method, Is.EqualTo("GET"));
        Assert.That(httpExchange.Request.Url, Is.EqualTo("http://cors-api-test.dev/allorigins"));
        Assert.That(httpExchange.Request.PostData, Is.Null);
        Assert.That(httpExchange.Request.Headers, Is.Not.Null);
        Assert.That(httpExchange.Request.Headers.Count, Is.EqualTo(3));

        var hostHeader =
            httpExchange.Request.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Host", StringComparison.OrdinalIgnoreCase));
        Assert.That(hostHeader, Is.Not.Null);
        Assert.That(hostHeader.IsHighlighted, Is.EqualTo(false));
        Assert.That(hostHeader.Value, Is.EqualTo("cors-api-test.dev"));

        var userAgentHeader =
            httpExchange.Request.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "User-Agent", StringComparison.OrdinalIgnoreCase));
        Assert.That(userAgentHeader, Is.Not.Null);
        Assert.That(userAgentHeader.IsHighlighted, Is.EqualTo(false));
        Assert.That(userAgentHeader.Value, Is.EqualTo("UA"));

        var originHeader =
            httpExchange.Request.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Origin", StringComparison.OrdinalIgnoreCase));
        Assert.That(originHeader, Is.Not.Null);
        Assert.That(originHeader.IsHighlighted, Is.EqualTo(true));
        Assert.That(originHeader.Value, Is.EqualTo("http://front-test.dev"));

        Assert.That(httpExchange.Response, Is.Not.Null);
        Assert.That(httpExchange.Response.Status, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(httpExchange.Response.Body, Is.EqualTo("open"));
        Assert.That(httpExchange.Response.Headers, Is.Not.Null);
        Assert.That(httpExchange.Response.Headers.Count, Is.EqualTo(1));

        var serverHeader =
            httpExchange.Response.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Server", StringComparison.OrdinalIgnoreCase));
        Assert.That(serverHeader, Is.Not.Null);
        Assert.That(serverHeader.IsHighlighted, Is.EqualTo(false));
        Assert.That(serverHeader.Value, Is.EqualTo("Kestrel"));
        
        Assert.That(result.SequenceDiagram, Is.EqualTo(sequenceDiagram));
        
        Assert.That(result.Summary, Is.Not.Null);
        Assert.That(result.Summary.IsPreflight, Is.EqualTo(false));
        
        Assert.That(result.Summary.Origin, Is.Not.Null);
        Assert.That(result.Summary.Origin.Name, Is.EqualTo("Origin"));
        Assert.That(result.Summary.Origin.Requested, Is.EqualTo("http://front-test.dev"));
        Assert.That(result.Summary.Origin.Received, Is.Null);
        Assert.That(result.Summary.Origin.isValid, Is.EqualTo(false));
        
        Assert.That(result.Summary.Method, Is.Not.Null);
        Assert.That(result.Summary.Method.Name, Is.EqualTo("Method"));
        Assert.That(result.Summary.Method.Requested, Is.Null);
        Assert.That(result.Summary.Method.Received, Is.Null);
        Assert.That(result.Summary.Method.isValid, Is.EqualTo(true));
        
        Assert.That(result.Summary.Headers, Is.Not.Null);
        Assert.That(result.Summary.Headers.Name, Is.EqualTo("Headers"));
        Assert.That(result.Summary.Headers.Requested, Is.Null);
        Assert.That(result.Summary.Headers.Received, Is.Null);
        Assert.That(result.Summary.Headers.isValid, Is.EqualTo(true));
    }

    [Test]
    public async Task TestConfigurationAsync_With_PreflightPayload()
    {
        string payload = @"const req = new XMLHttpRequest();
            req.addEventListener(""load"", function () {
                    console.log('OK');
                });
                req.open(""PUT"", ""http://cors-api-test.dev/allorigins"");
                req.setRequestHeader(""X-Custom-Header"", 1);
                req.send();";

        List<HttpExchange> httpExchanges = new List<HttpExchange>()
        {
            new()
            {
                Request = new Request()
                {
                    Method = "GET",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Host", Value = "front-test.dev" },
                        new() { IsHighlighted = false, Key = "User-Agent", Value = "UA" },
                    },
                    PostData = null,
                    Url = "http://front-test.dev"
                },
                Response = new Response()
                {
                    Body = "",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Server", Value = "Kestrel" },
                        new()
                        {
                            IsHighlighted = false, Key = "Set-Cookie", Value = "MyCookie=XXX; path=/"
                        }
                    },
                    Status = HttpStatusCode.OK
                }
            },
            new()
            {
                Request = new Request()
                {
                    Method = "OPTIONS",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Host", Value = "cors-api-test.dev" },
                        new() { IsHighlighted = false, Key = "User-Agent", Value = "UA" },
                        new() { IsHighlighted = false, Key = "Origin", Value = "http://front-test.dev" },
                        new() { IsHighlighted = false, Key = "Access-Control-Request-Method", Value = "PUT" },
                        new()
                        {
                            IsHighlighted = false, Key = "Access-Control-Request-Headers", Value = "x-custom-header"
                        },
                    },
                    PostData = null,
                    Url = "http://cors-api-test.dev/allorigins"
                },
                Response = new Response()
                {
                    Body = "open",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Server", Value = "Kestrel" },
                        new()
                        {
                            IsHighlighted = false, Key = "Access-Control-Allow-Origin", Value = "*"
                        },
                        new()
                        {
                            IsHighlighted = false, Key = "Access-Control-Allow-Headers", Value = "x-custom-header"
                        },
                        new()
                        {
                            IsHighlighted = false, Key = "Access-Control-Allow-Methods", Value = "GET,PUT"
                        }
                    },
                    Status = HttpStatusCode.NoContent
                }
            },
            new()
            {
                Request = new Request()
                {
                    Method = "PUT",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Host", Value = "cors-api-test.dev" },
                        new() { IsHighlighted = false, Key = "User-Agent", Value = "UA" },
                        new() { IsHighlighted = false, Key = "Origin", Value = "http://front-test.dev" },
                        new() { IsHighlighted = false, Key = "X-Custom-Header", Value = "1" }
                    },
                    PostData = null,
                    Url = "http://cors-api-test.dev/allorigins"
                },
                Response = new Response()
                {
                    Body = "open",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Server", Value = "Kestrel" },
                        new()
                        {
                            IsHighlighted = false, Key = "Access-Control-Allow-Origin", Value = "*"
                        }
                    },
                    Status = HttpStatusCode.OK
                }
            }
        };

        List<ConsoleMessage> consoleMessages = new List<ConsoleMessage>()
        {
            new() { Text = "OK", Type = "Log" }
        };

        var sequenceDiagram =
            "sequenceDiagram\nBrowser->>+API:#8194;#8194;#8194;OPTIONS http://cors-api-test.dev/allorigins\nNote over Browser,API:Access-Control-Request-Method: PUT<br />Access-Control-Request-Headers: x-custom-header<br />Origin: http://front-test.dev\nAPI->>API:Check CORS configuration\nAPI-->>-Browser:204 NoContent\nNote over Browser,API:Access-Control-Allow-Headers: x-custom-header<br />Access-Control-Allow-Methods: GET,PUT<br />Access-Control-Allow-Origin: *\nBrowser->>+Browser:Interpret API result\nBrowser->>+API:#8194;#8194;#8194;PUT http://cors-api-test.dev/allorigins\nNote over Browser,API:Origin: http://front-test.dev\nAPI->>API:Check CORS configuration\nAPI-->>-Browser:200 OK\nNote over Browser,API:Access-Control-Allow-Origin: *\nBrowser->>+Browser:Interpret API result\n";

        _headlessBrowserProviderMock.Setup(provider => provider.InitializeBrowserAsync(It.IsAny<string>()));
        _headlessBrowserProviderMock.Setup(provider => provider.GoToAsync(_configuration["HeadlessFrontUrl"]));
        _headlessBrowserProviderMock.Setup(provider => provider.EvaluateExpressionAsync(payload));
        _headlessBrowserProviderMock.Setup(provider => provider.WaitForTimeoutAsync(3000));
        _headlessBrowserProviderMock.Setup(provider => provider.ConsoleMessages).Returns(consoleMessages);
        _diagramProviderMock
            .Setup(provider =>
                provider.BuildSequenceDiagramFromHttpExchanges(It.IsAny<List<HttpExchange>>(), "Browser", "API"))
            .Returns(sequenceDiagram);

        _proxyRepositoryMock.Setup(repo => repo.GetHttpExchangesByCorrelationIdAsync(It.IsAny<string>()))
            .ReturnsAsync(httpExchanges);

        var result = await _corsService.TestConfigurationAsync(payload);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ConsoleMessages, Is.Not.Null);
        Assert.That(result.HttpExchanges, Is.Not.Null);
        Assert.That(result.SequenceDiagram, Is.Not.Null);
        Assert.That(result.IsInError, Is.EqualTo(false));

        Assert.That(result.ConsoleMessages.Count, Is.EqualTo(1));
        Assert.That(result.ConsoleMessages.First().Text, Is.EqualTo("OK"));
        Assert.That(result.ConsoleMessages.First().Type, Is.EqualTo("Log"));

        Assert.That(result.HttpExchanges.Count, Is.EqualTo(2));

        {
            var preflightHttpExchange = result.HttpExchanges.First();

            Assert.That(preflightHttpExchange.Request, Is.Not.Null);
            Assert.That(preflightHttpExchange.Request.Method, Is.EqualTo("OPTIONS"));
            Assert.That(preflightHttpExchange.Request.Url, Is.EqualTo("http://cors-api-test.dev/allorigins"));
            Assert.That(preflightHttpExchange.Request.PostData, Is.Null);
            Assert.That(preflightHttpExchange.Request.Headers, Is.Not.Null);
            Assert.That(preflightHttpExchange.Request.Headers.Count, Is.EqualTo(5));

            var hostHeader =
                preflightHttpExchange.Request.Headers.FirstOrDefault(h =>
                    string.Equals(h.Key, "Host", StringComparison.OrdinalIgnoreCase));
            Assert.That(hostHeader, Is.Not.Null);
            Assert.That(hostHeader.IsHighlighted, Is.EqualTo(false));
            Assert.That(hostHeader.Value, Is.EqualTo("cors-api-test.dev"));

            var userAgentHeader =
                preflightHttpExchange.Request.Headers.FirstOrDefault(h =>
                    string.Equals(h.Key, "User-Agent", StringComparison.OrdinalIgnoreCase));
            Assert.That(userAgentHeader, Is.Not.Null);
            Assert.That(userAgentHeader.IsHighlighted, Is.EqualTo(false));
            Assert.That(userAgentHeader.Value, Is.EqualTo("UA"));

            var originHeader =
                preflightHttpExchange.Request.Headers.FirstOrDefault(h =>
                    string.Equals(h.Key, "Origin", StringComparison.OrdinalIgnoreCase));
            Assert.That(originHeader, Is.Not.Null);
            Assert.That(originHeader.IsHighlighted, Is.EqualTo(true));
            Assert.That(originHeader.Value, Is.EqualTo("http://front-test.dev"));

            var accessControlRequestMethodHeader =
                preflightHttpExchange.Request.Headers.FirstOrDefault(h =>
                    string.Equals(h.Key, "Access-Control-Request-Method", StringComparison.OrdinalIgnoreCase));
            Assert.That(accessControlRequestMethodHeader, Is.Not.Null);
            Assert.That(accessControlRequestMethodHeader.IsHighlighted, Is.EqualTo(true));
            Assert.That(accessControlRequestMethodHeader.Value, Is.EqualTo("PUT"));

            var accessControlRequestHeadersHeader =
                preflightHttpExchange.Request.Headers.FirstOrDefault(h =>
                    string.Equals(h.Key, "Access-Control-Request-Headers", StringComparison.OrdinalIgnoreCase));
            Assert.That(accessControlRequestHeadersHeader, Is.Not.Null);
            Assert.That(accessControlRequestHeadersHeader.IsHighlighted, Is.EqualTo(true));
            Assert.That(accessControlRequestHeadersHeader.Value, Is.EqualTo("x-custom-header"));

            Assert.That(preflightHttpExchange.Response, Is.Not.Null);
            Assert.That(preflightHttpExchange.Response.Status, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(preflightHttpExchange.Response.Body, Is.EqualTo("open"));
            Assert.That(preflightHttpExchange.Response.Headers, Is.Not.Null);
            Assert.That(preflightHttpExchange.Response.Headers.Count, Is.EqualTo(4));

            var serverHeader =
                preflightHttpExchange.Response.Headers.FirstOrDefault(h =>
                    string.Equals(h.Key, "Server", StringComparison.OrdinalIgnoreCase));
            Assert.That(serverHeader, Is.Not.Null);
            Assert.That(serverHeader.IsHighlighted, Is.EqualTo(false));
            Assert.That(serverHeader.Value, Is.EqualTo("Kestrel"));

            var accessAllowOriginHeader =
                preflightHttpExchange.Response.Headers.FirstOrDefault(h =>
                    string.Equals(h.Key, "Access-Control-Allow-Origin", StringComparison.OrdinalIgnoreCase));
            Assert.That(accessAllowOriginHeader, Is.Not.Null);
            Assert.That(accessAllowOriginHeader.IsHighlighted, Is.EqualTo(true));
            Assert.That(accessAllowOriginHeader.Value, Is.EqualTo("*"));

            var accessControlAllowHeadersHeader =
                preflightHttpExchange.Response.Headers.FirstOrDefault(h =>
                    string.Equals(h.Key, "Access-Control-Allow-Headers", StringComparison.OrdinalIgnoreCase));
            Assert.That(accessControlAllowHeadersHeader, Is.Not.Null);
            Assert.That(accessControlAllowHeadersHeader.IsHighlighted, Is.EqualTo(true));
            Assert.That(accessControlAllowHeadersHeader.Value, Is.EqualTo("x-custom-header"));

            var accessControlAllowMethodsHeader =
                preflightHttpExchange.Response.Headers.FirstOrDefault(h =>
                    string.Equals(h.Key, "Access-Control-Allow-Methods", StringComparison.OrdinalIgnoreCase));
            Assert.That(accessControlAllowMethodsHeader, Is.Not.Null);
            Assert.That(accessControlAllowMethodsHeader.IsHighlighted, Is.EqualTo(true));
            Assert.That(accessControlAllowMethodsHeader.Value, Is.EqualTo("GET,PUT"));
        }
        {
            var httpExchange = result.HttpExchanges.Last();

            Assert.That(httpExchange.Request, Is.Not.Null);
            Assert.That(httpExchange.Request.Method, Is.EqualTo("PUT"));
            Assert.That(httpExchange.Request.Url, Is.EqualTo("http://cors-api-test.dev/allorigins"));
            Assert.That(httpExchange.Request.PostData, Is.Null);
            Assert.That(httpExchange.Request.Headers, Is.Not.Null);
            Assert.That(httpExchange.Request.Headers.Count, Is.EqualTo(4));

            var hostHeader =
                httpExchange.Request.Headers.FirstOrDefault(h =>
                    string.Equals(h.Key, "Host", StringComparison.OrdinalIgnoreCase));
            Assert.That(hostHeader, Is.Not.Null);
            Assert.That(hostHeader.IsHighlighted, Is.EqualTo(false));
            Assert.That(hostHeader.Value, Is.EqualTo("cors-api-test.dev"));

            var userAgentHeader =
                httpExchange.Request.Headers.FirstOrDefault(h =>
                    string.Equals(h.Key, "User-Agent", StringComparison.OrdinalIgnoreCase));
            Assert.That(userAgentHeader, Is.Not.Null);
            Assert.That(userAgentHeader.IsHighlighted, Is.EqualTo(false));
            Assert.That(userAgentHeader.Value, Is.EqualTo("UA"));

            var originHeader =
                httpExchange.Request.Headers.FirstOrDefault(h =>
                    string.Equals(h.Key, "Origin", StringComparison.OrdinalIgnoreCase));
            Assert.That(originHeader, Is.Not.Null);
            Assert.That(originHeader.IsHighlighted, Is.EqualTo(true));
            Assert.That(originHeader.Value, Is.EqualTo("http://front-test.dev"));

            Assert.That(httpExchange.Response, Is.Not.Null);
            Assert.That(httpExchange.Response.Status, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(httpExchange.Response.Body, Is.EqualTo("open"));
            Assert.That(httpExchange.Response.Headers, Is.Not.Null);
            Assert.That(httpExchange.Response.Headers.Count, Is.EqualTo(2));

            var serverHeader =
                httpExchange.Response.Headers.FirstOrDefault(h =>
                    string.Equals(h.Key, "Server", StringComparison.OrdinalIgnoreCase));
            Assert.That(serverHeader, Is.Not.Null);
            Assert.That(serverHeader.IsHighlighted, Is.EqualTo(false));
            Assert.That(serverHeader.Value, Is.EqualTo("Kestrel"));

            var accessAllowOriginHeader =
                httpExchange.Response.Headers.FirstOrDefault(h =>
                    string.Equals(h.Key, "Access-Control-Allow-Origin", StringComparison.OrdinalIgnoreCase));
            Assert.That(accessAllowOriginHeader, Is.Not.Null);
            Assert.That(accessAllowOriginHeader.IsHighlighted, Is.EqualTo(true));
            Assert.That(accessAllowOriginHeader.Value, Is.EqualTo("*"));
        }

        Assert.That(result.SequenceDiagram, Is.EqualTo(sequenceDiagram));
        
        Assert.That(result.Summary, Is.Not.Null);
        Assert.That(result.Summary.IsPreflight, Is.EqualTo(true));
        
        Assert.That(result.Summary.Origin, Is.Not.Null);
        Assert.That(result.Summary.Origin.Name, Is.EqualTo("Origin"));
        Assert.That(result.Summary.Origin.Requested, Is.EqualTo("http://front-test.dev"));
        Assert.That(result.Summary.Origin.Received, Is.EqualTo("*"));
        Assert.That(result.Summary.Origin.isValid, Is.EqualTo(true));
        
        Assert.That(result.Summary.Method, Is.Not.Null);
        Assert.That(result.Summary.Method.Name, Is.EqualTo("Method"));
        Assert.That(result.Summary.Method.Requested, Is.EqualTo("PUT"));
        Assert.That(result.Summary.Method.Received, Is.EqualTo("GET,PUT"));
        Assert.That(result.Summary.Method.isValid, Is.EqualTo(true));
        
        Assert.That(result.Summary.Headers, Is.Not.Null);
        Assert.That(result.Summary.Headers.Name, Is.EqualTo("Headers"));
        Assert.That(result.Summary.Headers.Requested, Is.EqualTo("x-custom-header"));
        Assert.That(result.Summary.Headers.Received, Is.EqualTo("x-custom-header"));
        Assert.That(result.Summary.Headers.isValid, Is.EqualTo(true));
    }

    [Test]
    public async Task TestConfigurationAsync_With_PreflightPayload_And_ForbiddenCustomHeader()
    {
        string payload = @"const req = new XMLHttpRequest();
            req.addEventListener(""load"", function () {
                    console.log('OK');
                });
                req.open(""PUT"", ""http://cors-api-test.dev/allorigins"");
                req.setRequestHeader(""X-Other-Custom-Header"", 1);
                req.send();";

        List<HttpExchange> httpExchanges = new List<HttpExchange>()
        {
            new()
            {
                Request = new Request()
                {
                    Method = "GET",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Host", Value = "front-test.dev" },
                        new() { IsHighlighted = false, Key = "User-Agent", Value = "UA" },
                    },
                    PostData = null,
                    Url = "http://front-test.dev"
                },
                Response = new Response()
                {
                    Body = "",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Server", Value = "Kestrel" },
                        new()
                        {
                            IsHighlighted = false, Key = "Set-Cookie", Value = "MyCookie=XXX; path=/"
                        }
                    },
                    Status = HttpStatusCode.OK
                }
            },
            new()
            {
                Request = new Request()
                {
                    Method = "OPTIONS",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Host", Value = "cors-api-test.dev" },
                        new() { IsHighlighted = false, Key = "User-Agent", Value = "UA" },
                        new() { IsHighlighted = false, Key = "Origin", Value = "http://front-test.dev" },
                        new() { IsHighlighted = false, Key = "Access-Control-Request-Method", Value = "PUT" },
                        new()
                        {
                            IsHighlighted = false, Key = "Access-Control-Request-Headers",
                            Value = "x-other-custom-header"
                        },
                    },
                    PostData = null,
                    Url = "http://cors-api-test.dev/allorigins"
                },
                Response = new Response()
                {
                    Body = "open",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Server", Value = "Kestrel" },
                        new()
                        {
                            IsHighlighted = false, Key = "Access-Control-Allow-Origin", Value = "*"
                        },
                        new()
                        {
                            IsHighlighted = false, Key = "Access-Control-Allow-Headers", Value = "x-custom-header"
                        },
                        new()
                        {
                            IsHighlighted = false, Key = "Access-Control-Allow-Methods", Value = "GET,PUT"
                        }
                    },
                    Status = HttpStatusCode.NoContent
                }
            },
        };

        List<ConsoleMessage> consoleMessages = new List<ConsoleMessage>()
        {
            new() { Text = "Failed", Type = "Error" },
            new() { Text = "Failed to load resource: net::ERR_FAILED", Type = "Error" }
        };

        var sequenceDiagram =
            "sequenceDiagram\nBrowser->>+API:#8194;#8194;#8194;OPTIONS http://cors-api-test.dev/allorigins\nNote over Browser,API:Access-Control-Request-Method: PUT<br />Access-Control-Request-Headers: x-custom-header<br />Origin: http://front-test.dev\nAPI->>API:Check CORS configuration\nAPI-->>-Browser:204 NoContent\nNote over Browser,API:Access-Control-Allow-Headers: x-custom-header<br />Access-Control-Allow-Methods: GET,PUT<br />Access-Control-Allow-Origin: *\nBrowser->>+Browser:Interpret API result\nBrowser->>+API:#8194;#8194;#8194;PUT http://cors-api-test.dev/allorigins\nNote over Browser,API:Origin: http://front-test.dev\nAPI->>API:Check CORS configuration\nAPI-->>-Browser:200 OK\nNote over Browser,API:Access-Control-Allow-Origin: *\nBrowser->>+Browser:Interpret API result\n";

        _headlessBrowserProviderMock.Setup(provider => provider.InitializeBrowserAsync(It.IsAny<string>()));
        _headlessBrowserProviderMock.Setup(provider => provider.GoToAsync(_configuration["HeadlessFrontUrl"]));
        _headlessBrowserProviderMock.Setup(provider => provider.EvaluateExpressionAsync(payload));
        _headlessBrowserProviderMock.Setup(provider => provider.WaitForTimeoutAsync(3000));
        _headlessBrowserProviderMock.Setup(provider => provider.ConsoleMessages).Returns(consoleMessages);
        _diagramProviderMock
            .Setup(provider =>
                provider.BuildSequenceDiagramFromHttpExchanges(It.IsAny<List<HttpExchange>>(), "Browser", "API"))
            .Returns(sequenceDiagram);

        _proxyRepositoryMock.Setup(repo => repo.GetHttpExchangesByCorrelationIdAsync(It.IsAny<string>()))
            .ReturnsAsync(httpExchanges);

        var result = await _corsService.TestConfigurationAsync(payload);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ConsoleMessages, Is.Not.Null);
        Assert.That(result.HttpExchanges, Is.Not.Null);
        Assert.That(result.SequenceDiagram, Is.Not.Null);
        Assert.That(result.IsInError, Is.EqualTo(false));
        Assert.That(result.Summary.IsPreflight, Is.EqualTo(true));

        Assert.That(result.ConsoleMessages.Count, Is.EqualTo(2));
        Assert.That(result.ConsoleMessages.First().Text, Is.EqualTo("Failed"));
        Assert.That(result.ConsoleMessages.First().Type, Is.EqualTo("Error"));
        Assert.That(result.ConsoleMessages.Last().Text, Is.EqualTo("Failed to load resource: net::ERR_FAILED"));
        Assert.That(result.ConsoleMessages.Last().Type, Is.EqualTo("Error"));

        Assert.That(result.HttpExchanges.Count, Is.EqualTo(1));

        var preflightHttpExchange = result.HttpExchanges.First();

        Assert.That(preflightHttpExchange.Request, Is.Not.Null);
        Assert.That(preflightHttpExchange.Request.Method, Is.EqualTo("OPTIONS"));
        Assert.That(preflightHttpExchange.Request.Url, Is.EqualTo("http://cors-api-test.dev/allorigins"));
        Assert.That(preflightHttpExchange.Request.PostData, Is.Null);
        Assert.That(preflightHttpExchange.Request.Headers, Is.Not.Null);
        Assert.That(preflightHttpExchange.Request.Headers.Count, Is.EqualTo(5));

        var hostHeader =
            preflightHttpExchange.Request.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Host", StringComparison.OrdinalIgnoreCase));
        Assert.That(hostHeader, Is.Not.Null);
        Assert.That(hostHeader.IsHighlighted, Is.EqualTo(false));
        Assert.That(hostHeader.Value, Is.EqualTo("cors-api-test.dev"));

        var userAgentHeader =
            preflightHttpExchange.Request.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "User-Agent", StringComparison.OrdinalIgnoreCase));
        Assert.That(userAgentHeader, Is.Not.Null);
        Assert.That(userAgentHeader.IsHighlighted, Is.EqualTo(false));
        Assert.That(userAgentHeader.Value, Is.EqualTo("UA"));

        var originHeader =
            preflightHttpExchange.Request.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Origin", StringComparison.OrdinalIgnoreCase));
        Assert.That(originHeader, Is.Not.Null);
        Assert.That(originHeader.IsHighlighted, Is.EqualTo(true));
        Assert.That(originHeader.Value, Is.EqualTo("http://front-test.dev"));

        var accessControlRequestMethodHeader =
            preflightHttpExchange.Request.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Access-Control-Request-Method", StringComparison.OrdinalIgnoreCase));
        Assert.That(accessControlRequestMethodHeader, Is.Not.Null);
        Assert.That(accessControlRequestMethodHeader.IsHighlighted, Is.EqualTo(true));
        Assert.That(accessControlRequestMethodHeader.Value, Is.EqualTo("PUT"));

        var accessControlRequestHeadersHeader =
            preflightHttpExchange.Request.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Access-Control-Request-Headers", StringComparison.OrdinalIgnoreCase));
        Assert.That(accessControlRequestHeadersHeader, Is.Not.Null);
        Assert.That(accessControlRequestHeadersHeader.IsHighlighted, Is.EqualTo(true));
        Assert.That(accessControlRequestHeadersHeader.Value, Is.EqualTo("x-other-custom-header"));

        Assert.That(preflightHttpExchange.Response, Is.Not.Null);
        Assert.That(preflightHttpExchange.Response.Status, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(preflightHttpExchange.Response.Body, Is.EqualTo("open"));
        Assert.That(preflightHttpExchange.Response.Headers, Is.Not.Null);
        Assert.That(preflightHttpExchange.Response.Headers.Count, Is.EqualTo(4));

        var serverHeader =
            preflightHttpExchange.Response.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Server", StringComparison.OrdinalIgnoreCase));
        Assert.That(serverHeader, Is.Not.Null);
        Assert.That(serverHeader.IsHighlighted, Is.EqualTo(false));
        Assert.That(serverHeader.Value, Is.EqualTo("Kestrel"));

        var accessAllowOriginHeader =
            preflightHttpExchange.Response.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Access-Control-Allow-Origin", StringComparison.OrdinalIgnoreCase));
        Assert.That(accessAllowOriginHeader, Is.Not.Null);
        Assert.That(accessAllowOriginHeader.IsHighlighted, Is.EqualTo(true));
        Assert.That(accessAllowOriginHeader.Value, Is.EqualTo("*"));

        var accessControlAllowHeadersHeader =
            preflightHttpExchange.Response.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Access-Control-Allow-Headers", StringComparison.OrdinalIgnoreCase));
        Assert.That(accessControlAllowHeadersHeader, Is.Not.Null);
        Assert.That(accessControlAllowHeadersHeader.IsHighlighted, Is.EqualTo(true));
        Assert.That(accessControlAllowHeadersHeader.Value, Is.EqualTo("x-custom-header"));

        var accessControlAllowMethodsHeader =
            preflightHttpExchange.Response.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Access-Control-Allow-Methods", StringComparison.OrdinalIgnoreCase));
        Assert.That(accessControlAllowMethodsHeader, Is.Not.Null);
        Assert.That(accessControlAllowMethodsHeader.IsHighlighted, Is.EqualTo(true));
        Assert.That(accessControlAllowMethodsHeader.Value, Is.EqualTo("GET,PUT"));

        Assert.That(result.SequenceDiagram, Is.EqualTo(sequenceDiagram));
        
        Assert.That(result.Summary, Is.Not.Null);
        Assert.That(result.Summary.IsPreflight, Is.EqualTo(true));
        
        Assert.That(result.Summary.Origin, Is.Not.Null);
        Assert.That(result.Summary.Origin.Name, Is.EqualTo("Origin"));
        Assert.That(result.Summary.Origin.Requested, Is.EqualTo("http://front-test.dev"));
        Assert.That(result.Summary.Origin.Received, Is.EqualTo("*"));
        Assert.That(result.Summary.Origin.isValid, Is.EqualTo(true));
        
        Assert.That(result.Summary.Method, Is.Not.Null);
        Assert.That(result.Summary.Method.Name, Is.EqualTo("Method"));
        Assert.That(result.Summary.Method.Requested, Is.EqualTo("PUT"));
        Assert.That(result.Summary.Method.Received, Is.EqualTo("GET,PUT"));
        Assert.That(result.Summary.Method.isValid, Is.EqualTo(true));
        
        Assert.That(result.Summary.Headers, Is.Not.Null);
        Assert.That(result.Summary.Headers.Name, Is.EqualTo("Headers"));
        Assert.That(result.Summary.Headers.Requested, Is.EqualTo("x-other-custom-header"));
        Assert.That(result.Summary.Headers.Received, Is.EqualTo("x-custom-header"));
        Assert.That(result.Summary.Headers.isValid, Is.EqualTo(false));
    }
    
    [Test]
    public async Task TestConfigurationAsync_With_PreflightPayload_And_NotConfiguredCustomHeader()
    {
        string payload = @"const req = new XMLHttpRequest();
            req.addEventListener(""load"", function () {
                    console.log('OK');
                });
                req.open(""PUT"", ""http://cors-api-test.dev/allorigins"");
                req.setRequestHeader(""X-Other-Custom-Header"", 1);
                req.send();";

        List<HttpExchange> httpExchanges = new List<HttpExchange>()
        {
            new()
            {
                Request = new Request()
                {
                    Method = "GET",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Host", Value = "front-test.dev" },
                        new() { IsHighlighted = false, Key = "User-Agent", Value = "UA" },
                    },
                    PostData = null,
                    Url = "http://front-test.dev"
                },
                Response = new Response()
                {
                    Body = "",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Server", Value = "Kestrel" },
                        new()
                        {
                            IsHighlighted = false, Key = "Set-Cookie", Value = "MyCookie=XXX; path=/"
                        }
                    },
                    Status = HttpStatusCode.OK
                }
            },
            new()
            {
                Request = new Request()
                {
                    Method = "OPTIONS",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Host", Value = "cors-api-test.dev" },
                        new() { IsHighlighted = false, Key = "User-Agent", Value = "UA" },
                        new() { IsHighlighted = false, Key = "Origin", Value = "http://front-test.dev" },
                        new() { IsHighlighted = false, Key = "Access-Control-Request-Method", Value = "PUT" },
                        new()
                        {
                            IsHighlighted = false, Key = "Access-Control-Request-Headers",
                            Value = "x-other-custom-header"
                        },
                    },
                    PostData = null,
                    Url = "http://cors-api-test.dev/allorigins"
                },
                Response = new Response()
                {
                    Body = "open",
                    Headers = new List<Header>()
                    {
                        new() { IsHighlighted = false, Key = "Server", Value = "Kestrel" },
                        new()
                        {
                            IsHighlighted = false, Key = "Access-Control-Allow-Origin", Value = "*"
                        },
                        new()
                        {
                            IsHighlighted = false, Key = "Access-Control-Allow-Methods", Value = "GET,PUT"
                        }
                    },
                    Status = HttpStatusCode.NoContent
                }
            },
        };

        List<ConsoleMessage> consoleMessages = new List<ConsoleMessage>()
        {
            new() { Text = "Failed", Type = "Error" },
            new() { Text = "Failed to load resource: net::ERR_FAILED", Type = "Error" }
        };

        var sequenceDiagram =
            "sequenceDiagram\nBrowser->>+API:#8194;#8194;#8194;OPTIONS http://cors-api-test.dev/allorigins\nNote over Browser,API:Access-Control-Request-Method: PUT<br />Access-Control-Request-Headers: x-custom-header<br />Origin: http://front-test.dev\nAPI->>API:Check CORS configuration\nAPI-->>-Browser:204 NoContent\nNote over Browser,API:Access-Control-Allow-Headers: x-custom-header<br />Access-Control-Allow-Methods: GET,PUT<br />Access-Control-Allow-Origin: *\nBrowser->>+Browser:Interpret API result\nBrowser->>+API:#8194;#8194;#8194;PUT http://cors-api-test.dev/allorigins\nNote over Browser,API:Origin: http://front-test.dev\nAPI->>API:Check CORS configuration\nAPI-->>-Browser:200 OK\nNote over Browser,API:Access-Control-Allow-Origin: *\nBrowser->>+Browser:Interpret API result\n";

        _headlessBrowserProviderMock.Setup(provider => provider.InitializeBrowserAsync(It.IsAny<string>()));
        _headlessBrowserProviderMock.Setup(provider => provider.GoToAsync(_configuration["HeadlessFrontUrl"]));
        _headlessBrowserProviderMock.Setup(provider => provider.EvaluateExpressionAsync(payload));
        _headlessBrowserProviderMock.Setup(provider => provider.WaitForTimeoutAsync(3000));
        _headlessBrowserProviderMock.Setup(provider => provider.ConsoleMessages).Returns(consoleMessages);
        _diagramProviderMock
            .Setup(provider =>
                provider.BuildSequenceDiagramFromHttpExchanges(It.IsAny<List<HttpExchange>>(), "Browser", "API"))
            .Returns(sequenceDiagram);

        _proxyRepositoryMock.Setup(repo => repo.GetHttpExchangesByCorrelationIdAsync(It.IsAny<string>()))
            .ReturnsAsync(httpExchanges);

        var result = await _corsService.TestConfigurationAsync(payload);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ConsoleMessages, Is.Not.Null);
        Assert.That(result.HttpExchanges, Is.Not.Null);
        Assert.That(result.SequenceDiagram, Is.Not.Null);
        Assert.That(result.IsInError, Is.EqualTo(false));
        Assert.That(result.Summary.IsPreflight, Is.EqualTo(true));

        Assert.That(result.ConsoleMessages.Count, Is.EqualTo(2));
        Assert.That(result.ConsoleMessages.First().Text, Is.EqualTo("Failed"));
        Assert.That(result.ConsoleMessages.First().Type, Is.EqualTo("Error"));
        Assert.That(result.ConsoleMessages.Last().Text, Is.EqualTo("Failed to load resource: net::ERR_FAILED"));
        Assert.That(result.ConsoleMessages.Last().Type, Is.EqualTo("Error"));

        Assert.That(result.HttpExchanges.Count, Is.EqualTo(1));

        var preflightHttpExchange = result.HttpExchanges.First();

        Assert.That(preflightHttpExchange.Request, Is.Not.Null);
        Assert.That(preflightHttpExchange.Request.Method, Is.EqualTo("OPTIONS"));
        Assert.That(preflightHttpExchange.Request.Url, Is.EqualTo("http://cors-api-test.dev/allorigins"));
        Assert.That(preflightHttpExchange.Request.PostData, Is.Null);
        Assert.That(preflightHttpExchange.Request.Headers, Is.Not.Null);
        Assert.That(preflightHttpExchange.Request.Headers.Count, Is.EqualTo(5));

        var hostHeader =
            preflightHttpExchange.Request.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Host", StringComparison.OrdinalIgnoreCase));
        Assert.That(hostHeader, Is.Not.Null);
        Assert.That(hostHeader.IsHighlighted, Is.EqualTo(false));
        Assert.That(hostHeader.Value, Is.EqualTo("cors-api-test.dev"));

        var userAgentHeader =
            preflightHttpExchange.Request.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "User-Agent", StringComparison.OrdinalIgnoreCase));
        Assert.That(userAgentHeader, Is.Not.Null);
        Assert.That(userAgentHeader.IsHighlighted, Is.EqualTo(false));
        Assert.That(userAgentHeader.Value, Is.EqualTo("UA"));

        var originHeader =
            preflightHttpExchange.Request.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Origin", StringComparison.OrdinalIgnoreCase));
        Assert.That(originHeader, Is.Not.Null);
        Assert.That(originHeader.IsHighlighted, Is.EqualTo(true));
        Assert.That(originHeader.Value, Is.EqualTo("http://front-test.dev"));

        var accessControlRequestMethodHeader =
            preflightHttpExchange.Request.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Access-Control-Request-Method", StringComparison.OrdinalIgnoreCase));
        Assert.That(accessControlRequestMethodHeader, Is.Not.Null);
        Assert.That(accessControlRequestMethodHeader.IsHighlighted, Is.EqualTo(true));
        Assert.That(accessControlRequestMethodHeader.Value, Is.EqualTo("PUT"));

        var accessControlRequestHeadersHeader =
            preflightHttpExchange.Request.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Access-Control-Request-Headers", StringComparison.OrdinalIgnoreCase));
        Assert.That(accessControlRequestHeadersHeader, Is.Not.Null);
        Assert.That(accessControlRequestHeadersHeader.IsHighlighted, Is.EqualTo(true));
        Assert.That(accessControlRequestHeadersHeader.Value, Is.EqualTo("x-other-custom-header"));

        Assert.That(preflightHttpExchange.Response, Is.Not.Null);
        Assert.That(preflightHttpExchange.Response.Status, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(preflightHttpExchange.Response.Body, Is.EqualTo("open"));
        Assert.That(preflightHttpExchange.Response.Headers, Is.Not.Null);
        Assert.That(preflightHttpExchange.Response.Headers.Count, Is.EqualTo(3));

        var serverHeader =
            preflightHttpExchange.Response.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Server", StringComparison.OrdinalIgnoreCase));
        Assert.That(serverHeader, Is.Not.Null);
        Assert.That(serverHeader.IsHighlighted, Is.EqualTo(false));
        Assert.That(serverHeader.Value, Is.EqualTo("Kestrel"));

        var accessAllowOriginHeader =
            preflightHttpExchange.Response.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Access-Control-Allow-Origin", StringComparison.OrdinalIgnoreCase));
        Assert.That(accessAllowOriginHeader, Is.Not.Null);
        Assert.That(accessAllowOriginHeader.IsHighlighted, Is.EqualTo(true));
        Assert.That(accessAllowOriginHeader.Value, Is.EqualTo("*"));
        
        var accessControlAllowMethodsHeader =
            preflightHttpExchange.Response.Headers.FirstOrDefault(h =>
                string.Equals(h.Key, "Access-Control-Allow-Methods", StringComparison.OrdinalIgnoreCase));
        Assert.That(accessControlAllowMethodsHeader, Is.Not.Null);
        Assert.That(accessControlAllowMethodsHeader.IsHighlighted, Is.EqualTo(true));
        Assert.That(accessControlAllowMethodsHeader.Value, Is.EqualTo("GET,PUT"));

        Assert.That(result.SequenceDiagram, Is.EqualTo(sequenceDiagram));
        
        Assert.That(result.Summary, Is.Not.Null);
        Assert.That(result.Summary.IsPreflight, Is.EqualTo(true));
        
        Assert.That(result.Summary.Origin, Is.Not.Null);
        Assert.That(result.Summary.Origin.Name, Is.EqualTo("Origin"));
        Assert.That(result.Summary.Origin.Requested, Is.EqualTo("http://front-test.dev"));
        Assert.That(result.Summary.Origin.Received, Is.EqualTo("*"));
        Assert.That(result.Summary.Origin.isValid, Is.EqualTo(true));
        
        Assert.That(result.Summary.Method, Is.Not.Null);
        Assert.That(result.Summary.Method.Name, Is.EqualTo("Method"));
        Assert.That(result.Summary.Method.Requested, Is.EqualTo("PUT"));
        Assert.That(result.Summary.Method.Received, Is.EqualTo("GET,PUT"));
        Assert.That(result.Summary.Method.isValid, Is.EqualTo(true));
        
        Assert.That(result.Summary.Headers, Is.Not.Null);
        Assert.That(result.Summary.Headers.Name, Is.EqualTo("Headers"));
        Assert.That(result.Summary.Headers.Requested, Is.EqualTo("x-other-custom-header"));
        Assert.That(result.Summary.Headers.Received, Is.Null);
        Assert.That(result.Summary.Headers.isValid, Is.EqualTo(false));
    }

    [Test]
    public async Task TestConfigurationAsync_With_NullPayload()
    {
        string payload = null;

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await _corsService.TestConfigurationAsync(payload);
        });
    }

    [Test]
    public async Task TestConfigurationAsync_With_Payload_And_NullProxyData()
    {
        string payload = @"const req = new XMLHttpRequest();
            req.addEventListener(""load"", function () {
                    console.log('OK');
                });
                req.open(""GET"", ""http://cors-api-test.dev/allorigins"");
                req.send();";

        List<HttpExchange> httpExchanges = null;

        List<ConsoleMessage> consoleMessages = new List<ConsoleMessage>()
        {
            new() { Text = "OK", Type = "Log" }
        };

        var sequenceDiagram =
            "sequenceDiagram\nBrowser->>+API:#8194;#8194;#8194;GET http://cors-api-test.dev/allorigins\nNote over Browser,API:Origin: http://front-test.dev\nAPI->>API:Check CORS configuration\nAPI-->>-Browser:200 OK\nNote over Browser,API:Access-Control-Allow-Origin: *\nBrowser->>+Browser:Interpret API result\n";

        _headlessBrowserProviderMock.Setup(provider => provider.InitializeBrowserAsync(It.IsAny<string>()));
        _headlessBrowserProviderMock.Setup(provider => provider.GoToAsync(_configuration["HeadlessFrontUrl"]));
        _headlessBrowserProviderMock.Setup(provider => provider.EvaluateExpressionAsync(payload));
        _headlessBrowserProviderMock.Setup(provider => provider.WaitForTimeoutAsync(3000));
        _headlessBrowserProviderMock.Setup(provider => provider.ConsoleMessages).Returns(consoleMessages);
        _diagramProviderMock
            .Setup(provider =>
                provider.BuildSequenceDiagramFromHttpExchanges(It.IsAny<List<HttpExchange>>(), "Browser", "API"))
            .Returns(sequenceDiagram);

        _proxyRepositoryMock.Setup(repo => repo.GetHttpExchangesByCorrelationIdAsync(It.IsAny<string>()))
            .ReturnsAsync(httpExchanges);

        Assert.ThrowsAsync<NullReferenceException>(async () =>
        {
            await _corsService.TestConfigurationAsync(payload);
        });
    }
}