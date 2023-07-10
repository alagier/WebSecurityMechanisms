using WebSecurityMechanisms.Models;

namespace WebSecurityMechanisms.Api.Providers.Interfaces;

public interface IDiagramProvider
{
    string BuildSequenceDiagramFromHttpExchanges(List<HttpExchange> httpExchanges, string actor1, string actor2);
}