using System.Net.Http.Headers;
using System.Net.Http.Json;
using SarasBlogg.Services;
using SarasBlogg.DTOs;

public class LikeAPIManager
{
    private readonly HttpClient _http;
    private readonly string _base;
    private readonly IAccessTokenStore _tokens;

    public LikeAPIManager(HttpClient http, IConfiguration cfg, IAccessTokenStore tokens)
    {
        _http = http;
        _base = (cfg["ApiBaseUrl"] ?? "").TrimEnd('/');
        _tokens = tokens;
    }

    private void EnsureAuth()
    {
        var token = _tokens.AccessToken; // byt namn om din store heter något annat
        if (!string.IsNullOrWhiteSpace(token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        else
            _http.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<LikeDto?> GetAsync(int bloggId)
    {
        EnsureAuth(); // ok att följa med även om endpointen är öppen
        return await _http.GetFromJsonAsync<LikeDto>($"{_base}/api/likes/{bloggId}");
    }

    public async Task<int> GetCountAsync(int bloggId)
    {
        var dto = await GetAsync(bloggId);
        return dto?.Count ?? 0;
    }

    public async Task<LikeDto?> AddAsync(int bloggId, string userId)
    {
        EnsureAuth();
        var resp = await _http.PostAsJsonAsync($"{_base}/api/likes",
            new LikeDto { BloggId = bloggId, UserId = userId }); // servern tar userId från claims

        Console.WriteLine($"[LikeAPI] POST /api/likes => {(int)resp.StatusCode} {resp.ReasonPhrase}");
        if (!resp.IsSuccessStatusCode) return null;

        return await resp.Content.ReadFromJsonAsync<LikeDto>();
    }

    public async Task<LikeDto?> RemoveAsync(int bloggId)
    {
        EnsureAuth();
        // servern ignorerar {userId} i route och använder claims
        var resp = await _http.DeleteAsync($"{_base}/api/likes/{bloggId}/_");

        Console.WriteLine($"[LikeAPI] DELETE /api/likes/{bloggId}/_ => {(int)resp.StatusCode} {resp.ReasonPhrase}");
        if (!resp.IsSuccessStatusCode) return null;

        return await resp.Content.ReadFromJsonAsync<LikeDto>();
    }
}
