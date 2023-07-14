// Create a new HttpListener to listen for requests on the specified URL

using System.IO.Compression;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using WebSecurityMechanisms.Models;

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
var configuration =  new ConfigurationBuilder()
    .AddJsonFile($"appsettings.json", true, true)
    .AddJsonFile($"appsettings.{environment}.json", true, true);
            
var config = configuration.Build();
var connectionString = config.GetConnectionString("DataConnection");
var proxyPort = config["ProxyPort"];

HttpListener listener = new HttpListener();
listener.Prefixes.Add($"http://*:{proxyPort}/");
listener.Start();
HttpClientHandler handler = new HttpClientHandler()
{
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
};

HttpClient httpClient = new HttpClient(handler);

Console.WriteLine($"Proxy start on port {proxyPort}");

while (true)
{
    try
    {
        StringBuilder sb = new StringBuilder();

        HttpListenerContext context = listener.GetContext();

        HttpListenerRequest receivedRequest = context.Request;
        HttpListenerResponse responseToSend = context.Response;

        HttpRequestMessage destinationRequest =
            new HttpRequestMessage(new HttpMethod(receivedRequest.HttpMethod), receivedRequest.RawUrl);

        foreach (string header in receivedRequest.Headers)
        {
            if (!string.Equals(header, "Content-Length", StringComparison.OrdinalIgnoreCase))
                destinationRequest.Headers.TryAddWithoutValidation(header, receivedRequest.Headers[header]);
        }

        sb.Append(receivedRequest.HttpMethod);
        sb.Append(" ");
        sb.Append(receivedRequest.RawUrl);
        sb.Append(" - " + receivedRequest.Headers["User-Agent"]);

        HttpResponseMessage destinationResponse = httpClient.Send(destinationRequest);

        responseToSend.StatusCode = (int)destinationResponse.StatusCode;

        foreach (var header in destinationResponse.Headers)
        {
            responseToSend.Headers.Add(header.Key, header.Value.First());
        }

        if (destinationResponse.Content != null)
        {
            foreach (var header in destinationResponse.Content.Headers)
            {
                responseToSend.Headers.Add(header.Key, header.Value.First());
            }
        }

        sb.Append(" - " + destinationResponse.StatusCode);

        Console.WriteLine(sb.ToString());

        using (Stream destinationStream = destinationResponse.Content.ReadAsStream())
        {
            destinationStream.CopyTo(responseToSend.OutputStream);
        }

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            var request = await Request.CreateAsync(destinationRequest);
            var response = await Response.CreateAsync(destinationResponse);

            var command = connection.CreateCommand();
            command.CommandText =
                @"INSERT INTO proxy_history (correlation_id, request, response)
               VALUES (@correlation_id, @request, @response)";

            command.Parameters.AddWithValue("@correlation_id", receivedRequest.Headers["User-Agent"]);
            command.Parameters.AddWithValue("@request", JsonSerializer.Serialize(request));
            command.Parameters.AddWithValue("@response", JsonSerializer.Serialize(response));

            await command.ExecuteScalarAsync();
        }

        responseToSend.Close();
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }
}

listener.Close();