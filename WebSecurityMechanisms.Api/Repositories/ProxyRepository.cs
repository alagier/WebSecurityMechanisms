using Microsoft.Data.Sqlite;
using WebSecurityMechanisms.Api.Repositories.Interfaces;
using WebSecurityMechanisms.Models;
using System.Text.Json;

namespace WebSecurityMechanisms.Api.Repositories;

public class ProxyRepository : IProxyRepository
{
    private readonly IConfiguration _configuration;

    public ProxyRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<List<HttpExchange>> GetHttpExchangesByCorrelationIdAsync(string correlationId)
    {
        List<HttpExchange> result = new List<HttpExchange>();

        using (var connection = new SqliteConnection(_configuration.GetConnectionString("DataConnection")))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
                @"SELECT * FROM proxy_history WHERE correlation_id = @correlationId";

            command.Parameters.AddWithValue("@correlationId", correlationId);

            SqliteDataReader reader = await command.ExecuteReaderAsync();

            while (reader.Read())
            {
                result.Add(new HttpExchange()
                {
                    Request = JsonSerializer.Deserialize<Request>((string)reader["request"]),
                    Response = JsonSerializer.Deserialize<Response>((string)reader["response"])
                });
            }
        }

        return result;
    }
}