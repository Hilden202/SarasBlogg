namespace SarasBlogg.Services
{
    public interface IFileHelper
    {
        Task<string> SaveImageAsync(IFormFile file, string folder);
        void DeleteImage(string imageName, string folder);
    }
}
