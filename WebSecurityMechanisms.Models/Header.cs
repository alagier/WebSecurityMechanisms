using System.Net;
using System.Net.Http.Headers;

namespace WebSecurityMechanisms.Models;

public class Header
{
    public string Key { get; set; }

    public string Value { get; set; }

    public bool IsHighlighted { get; set; }

    public static List<Header> Create(HttpHeaders headers)
    {
        if (headers == null)
            throw new ArgumentNullException(nameof(headers));

        var result = new List<Header>();

        foreach (var header in headers)
        {
            result.Add(new Header()
            {
                Key = header.Key,
                Value = string.Join(';', header.Value)
            });
        }
        
        return result;
    }
}