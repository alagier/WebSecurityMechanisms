using WebSecurityMechanisms.Models;

namespace WebSecurityMechanisms.Api.Repositories.Interfaces;

public interface IProxyRepository
{
    Task<List<HttpExchange>> GetHttpExchangesByCorrelationIdAsync(string correlationId);
}