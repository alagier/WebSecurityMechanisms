using System.Text;
using WebSecurityMechanisms.Api.Providers.Interfaces;
using WebSecurityMechanisms.Models;

namespace WebSecurityMechanisms.Api.Providers;

public class DiagramProvider : IDiagramProvider
{
    private readonly IConfiguration _configuration;
    private readonly List<string> _corsHeaders;
    
    public DiagramProvider(IConfiguration configuration)
    {
        _configuration = configuration;
        _corsHeaders = _configuration.GetSection("Cors:Headers").Get<List<string>>();
    }
    
    public string BuildSequenceDiagramFromHttpExchanges(List<HttpExchange> httpExchanges, string actor1, string actor2)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("sequenceDiagram");
        
        httpExchanges.ForEach(e =>
        {
            sb.AppendLine($"{actor1}->>+{actor2}:#8194;#8194;#8194;{e.Request.Method} {e.Request.Url}");

            int corsHeadersCountInRequest = _corsHeaders.Count(ch =>
                e.Request.Headers.Any(rh => string.Equals(rh.Key, ch, StringComparison.OrdinalIgnoreCase)));
            
            if (corsHeadersCountInRequest > 0)
            {
                sb.Append($"Note over {actor1},{actor2}:");
                int i = 1;
                e.Request.Headers.ForEach(h =>
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
            
            sb.AppendLine($"{actor2}->>{actor2}:Check CORS configuration");
            sb.AppendLine($"{actor2}-->>-{actor1}:{(int)e.Response.Status} {e.Response.Status}");
            
            int corsHeadersCountInResponse = _corsHeaders.Count(ch =>
                e.Response.Headers.Any(rh => string.Equals(rh.Key, ch, StringComparison.OrdinalIgnoreCase)));
            
            if (corsHeadersCountInResponse > 0)
            {
                sb.Append($"Note over {actor1},{actor2}:");
                var j = 1;
                e.Response.Headers.ForEach(h =>
                {
                    if (_corsHeaders.Any(ch => string.Equals(ch, h.Key, StringComparison.OrdinalIgnoreCase)))
                    {
                        sb.Append($"{h.Key}: {h.Value}");
                        
                        if (j == corsHeadersCountInRequest)
                            sb.Append("\n");
                        else if (j < corsHeadersCountInRequest)
                            sb.Append("<br />");
                        j++;
                    }
                });   
            }
            
            sb.AppendLine($"{actor1}->>+{actor1}:Interpret API result");
            
        });

        return sb.ToString();
    }
    
    private string PadBoth(string source, int length)
    {
        int spaces = length - source.Length;
        int padLeft = spaces/2 + source.Length;
        return source.PadLeft(padLeft, '-').PadRight(length, '-');

    }
}