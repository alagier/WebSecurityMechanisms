using System.Net;

namespace WebSecurityMechanisms.Models;

public class Response
{
    public List<Header> Headers { get; set; }

    public HttpStatusCode Status { get; set; }

    public string Body { get; set; }

    public bool IsInError
    {
        get
        {
            int intStatus = (int)Status;

            if (intStatus >= 400 && intStatus <= 600)
                return true;

            return false;
        }
    }
    
    public static async Task<Response> CreateAsync(HttpResponseMessage response)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));

        Response result = new Response();
        result.Headers = Header.Create(response.Headers);
        result.Body = await response.Content.ReadAsStringAsync();
        result.Status = response.StatusCode;

        return result;
    }
}