using Microsoft.Extensions.Configuration;
using Moq;
using WebSecurityMechanisms.Api.Providers.Interfaces;
using WebSecurityMechanisms.Api.Repositories.Interfaces;

namespace WebSecurityMechanisms.Api.Tests.CorsService;

public class GetPresetAsync
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
    public async Task GetPresetAsync_With_ExpectedParameters()
    {
        string preset = "simple-get";
        string endpoint = "/testorigins";
        string presetInitialContent = @"const req = new XMLHttpRequest();
            req.addEventListener(""load"", function () {
                    console.log('OK');
                });
                req.open(""GET"", ""<APIURL>"");
                req.send();";
        string presetFinalContent = @"const req = new XMLHttpRequest();
            req.addEventListener(""load"", function () {
                    console.log('OK');
                });
                req.open(""GET"", """ + _configuration["CorsApiUrl"] + endpoint + @""");
                req.send();";


        _corsRepositoryMock.Setup(repo => repo.GetPresetAsync(preset)).ReturnsAsync(presetInitialContent);

        var result = await _corsService.GetPresetAsync(preset, endpoint);

        Assert.That(result, Is.EqualTo(presetFinalContent));
    }

    [Test]
    public void GetPresetAsync_With_UnknownPreset()
    {
        string preset = "unknown";
        string endpoint = "/testorigins";

        _corsRepositoryMock.Setup(repo => repo.GetPresetAsync(preset)).Throws<FileNotFoundException>();

        var exception = Assert.ThrowsAsync<FileNotFoundException>(async () =>
        {
            await _corsService.GetPresetAsync(preset, endpoint);
        });
    }

    [Test]
    public void GetPresetAsync_With_NullPreset()
    {
        string preset = null;
        string endpoint = "/testorigins";

        _corsRepositoryMock.Setup(repo => repo.GetPresetAsync(preset)).Throws<ArgumentNullException>();

        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await _corsService.GetPresetAsync(preset, endpoint);
        });
    }

    [Test]
    public void GetPresetAsync_With_NullEndpoint()
    {
        string preset = "simple-get";
        string endpoint = null;

        _corsRepositoryMock.Setup(repo => repo.GetPresetAsync(preset)).Throws<ArgumentNullException>();

        var exception = Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await _corsService.GetPresetAsync(preset, endpoint);
        });
    }
}