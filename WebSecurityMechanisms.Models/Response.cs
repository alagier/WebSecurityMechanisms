using System.Net;

namespace WebSecurityMechanisms.Models;

public class Response
{
    public List<Header>? Headers { get; set; }

    public HttpStatusCode Status { get; set; }

    public string? Body { get; set; }

    public bool IsInError
    {
        get
        {
            var intStatus = (int)Status;

            return intStatus is >= 400 and <= 600;
        }
    }
    
    public static async Task<Response> CreateAsync(HttpResponseMessage response)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));

        var result = new Response
        {
            Headers = Header.Create(response.Headers),
            Body = await response.Content.ReadAsStringAsync(),
            Status = response.StatusCode
        };

        return result;
    }
}