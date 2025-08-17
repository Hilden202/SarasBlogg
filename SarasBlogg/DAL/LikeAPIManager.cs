// DAL/LikeAPIManager.cs
public class LikeAPIManager
{
    private readonly HttpClient _http;
    private readonly string _base;

    public LikeAPIManager(HttpClient http, IConfiguration cfg)
    {
        _http = http;
        _base = (cfg["ApiBaseUrl"] ?? "").TrimEnd('/');
    }

    public class LikeDto
    {
        public int BloggId { get; set; }
        public string UserId { get; set; } = "";
        public int Count { get; set; }
    }

    public async Task<int> GetCountAsync(int bloggId)
    {
        var res = await _http.GetFromJsonAsync<LikeDto>($"{_base}/api/likes/{bloggId}");
        return res?.Count ?? 0;
    }

    public async Task<LikeDto?> AddAsync(int bloggId, string userId)
    {
        var resp = await _http.PostAsJsonAsync($"{_base}/api/likes",
                       new LikeDto { BloggId = bloggId, UserId = userId });
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<LikeDto>();
    }
}
