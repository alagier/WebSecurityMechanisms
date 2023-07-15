using Microsoft.AspNetCore.Mvc;
using WebSecurityMechanisms.Models;
using WebSecurityMechanisms.Api.Services.Interfaces;
using Endpoint = WebSecurityMechanisms.Models.Endpoint;

namespace WebSecurityMechanisms.Api.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class CorsController : ControllerBase
{
    private readonly ICorsService _corsService;

    public CorsController(ICorsService corsService)
    {
        _corsService = corsService;
    }

    [HttpPost]
    public async Task<BrowserNavigationData> TestConfigurationAsync([FromBody] string payload)
    {
        return await _corsService.TestConfigurationAsync(payload);
    }

    public List<Preset> ListPresets()
    {
        return _corsService.ListPresets();
    }
    
    public List<Endpoint> ListEndpoints()
    {
        return _corsService.ListEndpoints();
    }
    
    public async Task<string> GetPresetAsync(string preset, string endpoint)
    {
        return await _corsService.GetPresetAsync(preset, endpoint);
    }
}