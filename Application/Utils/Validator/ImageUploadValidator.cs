using Microsoft.AspNetCore.Http;

namespace Application.Utils.Validator
{
    public static class ImageUploadValidator
    {
        private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB

        private static readonly string[] AllowedTypes =
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        public static void Validate(IFormFile file)
        {
            if (file == null)
                throw new ArgumentException("File is required.");

            if (file.Length == 0)
                throw new ArgumentException("File is empty.");

            if (file.Length > MAX_FILE_SIZE)
                throw new ArgumentException("File exceeds maximum size (5MB).");

            if (!AllowedTypes.Contains(file.ContentType))
                throw new ArgumentException("Invalid image type. Allowed: jpg, png, webp.");
        }
    }
}