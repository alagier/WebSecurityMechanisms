namespace WebSecurityMechanisms.Api.Repositories.Interfaces;

public interface ICorsRepository
{
    Task<string> GetPresetAsync(string preset);
}