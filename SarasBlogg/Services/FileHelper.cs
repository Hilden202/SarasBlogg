namespace SarasBlogg.Services
{
    public class FileHelper : IFileHelper
    {
        public async Task<string> SaveImageAsync(IFormFile file, string folder)
        {
            if (file == null) return null;

            string fileName = Random.Shared.Next(0, 1000000) + "_" + file.FileName;
            string filePath = Path.Combine("wwwroot", folder, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!); // säkerställ mapp finns

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return fileName;
        }

        public void DeleteImage(string imageName, string folder)
        {
            if (string.IsNullOrEmpty(imageName)) return;

            string filePath = Path.Combine("wwwroot", folder, imageName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
