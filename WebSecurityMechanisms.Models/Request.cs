namespace WebSecurityMechanisms.Models;

public class Request
{
    public string RequestId { get; set; }
    
    public string Method { get; set; }

    public object PostData { get; set; }

    public List<Header> Headers { get; set; }

    public string Url { get; set; }
    
    public static async Task<Request> CreateAsync(HttpRequestMessage request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        Request result = new Request();
        result.Headers = Header.Create(request.Headers);

        if (request.Content != null)
        {
            var contentHeaders = Header.Create(request.Content.Headers);
            result.Headers.AddRange(contentHeaders);
            
            result.PostData = await request.Content.ReadAsStringAsync();
        }
        
        result.Method = request.Method.Method;
        result.Url = request.RequestUri.OriginalString;

        return result;
    }
}