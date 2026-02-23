using System.Net.Http.Headers;
using System.Text.Json;

namespace Planova.API.Pages;

public sealed class ApiClient
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly IHttpContextAccessor _httpContextAccessor;

	public ApiClient(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
	{
		_httpClientFactory = httpClientFactory;
		_httpContextAccessor = httpContextAccessor;
	}

	private string GetBaseUrl()
	{
		var request = _httpContextAccessor.HttpContext?.Request
			?? throw new InvalidOperationException("HttpContext is not available.");
		return $"{request.Scheme}://{request.Host}";
	}

	public async Task<HttpResponseMessage> GetAsync(string path, string? jwt = null)
	{
		var client = _httpClientFactory.CreateClient();
		if (!string.IsNullOrEmpty(jwt))
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
		return await client.GetAsync($"{GetBaseUrl()}{path}");
	}

	public async Task<T?> GetJsonAsync<T>(string path, string? jwt = null)
	{
		var response = await GetAsync(path, jwt);
		if (!response.IsSuccessStatusCode)
			return default;
		var json = await response.Content.ReadAsStringAsync();
		return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
	}

	public async Task<HttpResponseMessage> PostJsonAsync<TBody>(string path, TBody body, string? jwt = null)
	{
		var client = _httpClientFactory.CreateClient();
		if (!string.IsNullOrEmpty(jwt))
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
		var content = new StringContent(
			JsonSerializer.Serialize(body),
			System.Text.Encoding.UTF8,
			"application/json");
		return await client.PostAsync($"{GetBaseUrl()}{path}", content);
	}

	public async Task<HttpResponseMessage> PutJsonAsync<TBody>(string path, TBody body, string? jwt = null)
	{
		var client = _httpClientFactory.CreateClient();
		if (!string.IsNullOrEmpty(jwt))
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
		var content = new StringContent(
			JsonSerializer.Serialize(body),
			System.Text.Encoding.UTF8,
			"application/json");
		return await client.PutAsync($"{GetBaseUrl()}{path}", content);
	}

	public async Task<HttpResponseMessage> DeleteAsync(string path, string? jwt = null)
	{
		var client = _httpClientFactory.CreateClient();
		if (!string.IsNullOrEmpty(jwt))
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
		return await client.DeleteAsync($"{GetBaseUrl()}{path}");
	}
}
