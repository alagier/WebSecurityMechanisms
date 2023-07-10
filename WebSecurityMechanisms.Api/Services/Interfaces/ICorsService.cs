using WebSecurityMechanisms.Models;
using Endpoint = WebSecurityMechanisms.Models.Endpoint;

namespace WebSecurityMechanisms.Api.Services.Interfaces;

public interface ICorsService
{
    Task<CorsBrowserNavigationData> TestConfigurationAsync(string payload);

    Task<String> GetPresetAsync(string preset, string endpoint);

    List<Preset> ListPresets();
    
    List<Endpoint> ListEndpoints();
}