using System.Text.Json;

namespace SarasBlogg.DAL
{
    public class CommentAPIManager
    {
        private static Uri BaseAddress = new Uri("https://localhost:44316/");

        public static async Task<List<Models.Comment>> GetAllCommentsAsync()
        {
            List<Models.Comment> comments = new();

            using (var client = new HttpClient())
            {
                client.BaseAddress = BaseAddress;
                HttpResponseMessage response = await client.GetAsync("api/Comment");
                if (response.IsSuccessStatusCode)
                {
                    string responsString = await response.Content.ReadAsStringAsync();
                    comments = JsonSerializer.Deserialize<List<Models.Comment>>(responsString);
                }
                return comments;
            }
        }
        public static async Task<Models.Comment> GetCommentAsync(int id)
        {
            Models.Comment comment = new();
            using (var client = new HttpClient())
            {
                client.BaseAddress = BaseAddress;
                HttpResponseMessage response = await client.GetAsync("api/Comment/" + id);
                if (response.IsSuccessStatusCode)
                {
                    string responsString = await response.Content.ReadAsStringAsync();
                    comment = JsonSerializer.Deserialize<Models.Comment>(responsString);
                }
                return comment;
            }
        }

        public static async Task SaveCommentAsync(Models.Comment comment)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = BaseAddress;
                var json = JsonSerializer.Serialize(comment);
                StringContent httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync("api/Comment", httpContent);
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

        public static async Task DeleteCommentAsync(int id)
        {
            using(var client = new HttpClient())
            {
                client.BaseAddress = BaseAddress;
                HttpResponseMessage response = await client.DeleteAsync("api/Comment/" + id);
            }
        }
    }
}
