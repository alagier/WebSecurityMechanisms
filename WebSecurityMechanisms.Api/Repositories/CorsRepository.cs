using WebSecurityMechanisms.Api.Repositories.Interfaces;

namespace WebSecurityMechanisms.Api.Repositories;

public class CorsRepository : ICorsRepository
{
    public async Task<string> GetPresetAsync(string preset)
    {
        using var sr = new StreamReader(Path.Combine("./Resources/Cors/Presets", $"{preset}.txt"));
        return await sr.ReadToEndAsync();
    }
}