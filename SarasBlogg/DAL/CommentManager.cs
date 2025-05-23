using System.Text.Json;

namespace SarasBlogg.DAL
{
    public class CommentManager
    {
        private static Uri BaseAdress = new Uri("https://localhost:44316/");

        public static async Task<List<Models.Comment>> GetAllComments()
        {
            List<Models.Comment> comments = new();

            using(var client = new HttpClient())
            {
                //client.BaseAddress = BaseAdress;
                //HttpResponseMessage response = await client.GetAsync("api/Comment");
                //if (response.IsSuccessStatusCode)
                //{
                //    string responsString = await response.Content.ReadAsStringAsync();
                //    comments = JsonSerializer.Deserialize<List<Models.Comment>>(responsString);
                //}
                return comments;
            }
        }
    }
}
