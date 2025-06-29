using System.Text.Json;
using SarasBlogg.Models;
using Microsoft.Extensions.Configuration;

namespace SarasBlogg.DAL
{
    public class CommentAPIManager
    {
        private readonly Uri _baseAddress;
        public CommentAPIManager(IConfiguration config)
        {
            _baseAddress = new Uri(config["ApiSettings:BaseAddress"]);
        }


        public async Task<List<Models.Comment>> GetAllCommentsAsync()
        {
            List<Models.Comment> comments = new();

            using (var client = new HttpClient())
            {
                client.BaseAddress = _baseAddress;
                HttpResponseMessage response = await client.GetAsync("api/Comment");
                if (response.IsSuccessStatusCode)
                {
                    string responsString = await response.Content.ReadAsStringAsync();
                    comments = JsonSerializer.Deserialize<List<Models.Comment>>(responsString);
                }
                return comments;
            }
        }
        public async Task<Models.Comment> GetCommentAsync(int id)
        {
            Models.Comment comment = new();
            using (var client = new HttpClient())
            {
                client.BaseAddress = _baseAddress;
                HttpResponseMessage response = await client.GetAsync("api/Comment/ById/" + id);
                if (response.IsSuccessStatusCode)
                {
                    string responsString = await response.Content.ReadAsStringAsync();
                    comment = JsonSerializer.Deserialize<Models.Comment>(responsString);
                }
                return comment;
            }
        }

        public async Task<string> SaveCommentAsync(Models.Comment comment) // La till string för att få tillbaka return.
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = _baseAddress;
                var json = JsonSerializer.Serialize(comment);
                StringContent httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("api/Comment", httpContent);

                if (response.IsSuccessStatusCode)
                {
                    return null;
                }
                else
                {
                    // Läs felmeddelande från API:t och returnera det
                    string errorMsg = await response.Content.ReadAsStringAsync();
                    return errorMsg;
                }
            }
        }

        public async Task DeleteCommentAsync(int id)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = _baseAddress;
                HttpResponseMessage response = await client.DeleteAsync("api/Comment/ById/" + id);
            }
        }

        public async Task DeleteCommentsAsync(int bloggId)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = _baseAddress;
                HttpResponseMessage response = await client.DeleteAsync("api/Comment/ByBlogg/" + bloggId);
            }
        }

        //public static async Task UpdateCommentAsync(Models.Comment comment)
        //{
        //    using (var client = new HttpClient())
        //    {
        //        client.BaseAddress = BaseAddress;
        //        var json = JsonSerializer.Serialize(comment);
        //        StringContent httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        //        HttpResponseMessage response = await client.PutAsync("api/Comment/" + comment.Id, httpContent);
        //    }
        //}
    }
}
