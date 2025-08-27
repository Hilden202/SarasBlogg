namespace SarasBlogg.Services
{
    public interface IAccessTokenStore
    {
        string? AccessToken { get; }
        void Set(string token);
        void Clear();
    }

    public class InMemoryAccessTokenStore : IAccessTokenStore
    {
        public string? AccessToken { get; private set; }
        public void Set(string token) => AccessToken = token;
        public void Clear() => AccessToken = null;
    }
}
