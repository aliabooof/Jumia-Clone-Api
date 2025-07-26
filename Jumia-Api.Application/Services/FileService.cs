using Jumia_Api.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Jumia_Api.Application.Services
{
    public class FileService : IFileService
    {
        private readonly string[] _allowedImageTypes = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly string[] _allowedDocTypes = { ".pdf", ".doc", ".docx", ".txt", ".xlsx", ".pptx" };
        private readonly string[] _allowedVoiceTypes = { ".mp3", ".wav", ".ogg", ".m4a" };
        public bool IsValidDocument(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return _allowedDocTypes.Contains(extension) && file.Length < 10 * 1024 * 1024;
        }

        public bool IsValidImage(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return _allowedImageTypes.Contains(extension) && file.Length < 10 * 1024 * 1024;
        }

        public bool IsValidVoice(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return _allowedVoiceTypes.Contains(extension) && file.Length < 25 * 1024 * 1024;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            var uploadsFolder = Path.Combine("wwwroot", "uploads", folder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/{folder}/{uniqueFileName}";
        }

        public void DeleteFile(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return; // Nothing to delete
            }

            // The URL path starts with /uploads/. We need to convert it to a physical path.
            // Example: /uploads/products/some-image.jpg -> wwwroot/uploads/products/some-image.jpg
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileUrl.TrimStart('/'));

            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (IOException ex)
                {
                    // Log the error: File might be in use, or permissions issue
                    Console.WriteLine($"Error deleting file {filePath}: {ex.Message}");
                    // You might want to re-throw or handle differently based on your error policy
                }
                catch (UnauthorizedAccessException ex)
                {
                    // Log the error: Permissions issue
                    Console.WriteLine($"Permissions error deleting file {filePath}: {ex.Message}");
                }
            }
            else
            {
                // Optionally log if the file doesn't exist, though it's often not an error
                // Console.WriteLine($"Attempted to delete file that does not exist: {filePath}");
            }
        }
    }
}
