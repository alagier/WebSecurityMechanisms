using System.Text;
using WebSecurityMechanisms.Api.Providers.Interfaces;
using WebSecurityMechanisms.Models;

namespace WebSecurityMechanisms.Api.Providers;

public class DiagramProvider : IDiagramProvider
{
    private readonly List<string>? _corsHeaders;
    
    public DiagramProvider(IConfiguration configuration)
    {
        _corsHeaders = configuration.GetSection("Cors:Headers").Get<List<string>>();
    }
    
    public string BuildSequenceDiagramFromHttpExchanges(List<HttpExchange> httpExchanges, string actor1, string actor2)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("sequenceDiagram");
        
        httpExchanges.ForEach(e =>
        {
            sb.AppendLine($"{actor1}->>+{actor2}:#8194;#8194;#8194;{e.Request?.Method} {e.Request?.Url}");

            if (_corsHeaders != null)
            {
                int corsHeadersCountInRequest = _corsHeaders.Count(ch =>
                    e.Request is { Headers: not null } && e.Request.Headers.Any(rh => string.Equals(rh.Key, ch, StringComparison.OrdinalIgnoreCase)));
            
                if (corsHeadersCountInRequest > 0)
                {
                    sb.Append($"Note over {actor1},{actor2}:");
                    int i = 1;
                    e.Request?.Headers?.ForEach(h =>
                    {
                        if (_corsHeaders.Any(ch => string.Equals(ch, h.Key, StringComparison.OrdinalIgnoreCase)))
                        {
                            sb.Append($"{h.Key}: {h.Value}");
                        
                            if (i == corsHeadersCountInRequest)
                                sb.Append("\n");
                            else if (i < corsHeadersCountInRequest)
                                sb.Append("<br />");
                            i++;
                        }
                    });   
                }
            }

            sb.AppendLine($"{actor2}->>{actor2}:Check CORS configuration");
            if (e.Response != null)
            {
                sb.AppendLine($"{actor2}-->>-{actor1}:{(int)e.Response.Status} {e.Response.Status}");

                if (_corsHeaders != null)
                {
                    int corsHeadersCountInResponse = _corsHeaders.Count(ch =>
                        e.Response.Headers != null && e.Response.Headers.Any(rh => string.Equals(rh.Key, ch, StringComparison.OrdinalIgnoreCase)));

                    if (corsHeadersCountInResponse > 0)
                    {
                        sb.Append($"Note over {actor1},{actor2}:");
                        var j = 1;
                        e.Response.Headers?.ForEach(h =>
                        {
                            if (_corsHeaders.Any(ch => string.Equals(ch, h.Key, StringComparison.OrdinalIgnoreCase)))
                            {
                                sb.Append($"{h.Key}: {h.Value}");

                                if (j == corsHeadersCountInResponse)
                                    sb.Append("\n");
                                else if (j < corsHeadersCountInResponse)
                                    sb.Append("<br />");
                                j++;
                            }
                        });
                    }
                }
            }

            sb.AppendLine($"{actor1}->>+{actor1}:Interpret API result");
            
        });

        return sb.ToString();
    }
}