
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDayWeb.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<T?> GetAsync<T>(string url, string? token = null)
    {
        using var request = CreateRequest(HttpMethod.Get, url, token);
        using var response = await _http.SendAsync(request);

        var json = await response.Content.ReadAsStringAsync();

        EnsureSuccess(response, json);

        if (string.IsNullOrWhiteSpace(json))
            return default;

        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    public async Task<HttpResponseMessage> PostAsync<T>(
        string url, T data, string? token = null)
    {
        return await SendWithBodyAsync(HttpMethod.Post, url, data, token);
    }

    public async Task<HttpResponseMessage> PutAsync<T>(
        string url, T data, string? token = null)
    {
        return await SendWithBodyAsync(HttpMethod.Put, url, data, token);
    }

    public async Task DeleteAsync(string url, string? token = null)
    {
        using var request = CreateRequest(HttpMethod.Delete, url, token);
        using var response = await _http.SendAsync(request);

        var body = await response.Content.ReadAsStringAsync();

        EnsureSuccess(response, body);
    }

    private async Task<HttpResponseMessage> SendWithBodyAsync<T>(
        HttpMethod method, string url, T data, string? token)
    {
        var json = JsonSerializer.Serialize(data);

        using var request = CreateRequest(method, url, token);

        request.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json"
        );

        var response = await _http.SendAsync(request);

        return response;
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method, string url, string? token)
    {
        var request = new HttpRequestMessage(method, url);

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    private void EnsureSuccess(
        HttpResponseMessage response, string responseBody)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"API request failed: {(int)response.StatusCode} " +
                $"{response.StatusCode}. Details: {responseBody}",
                null,
                response.StatusCode
            );
        }
    }
}
