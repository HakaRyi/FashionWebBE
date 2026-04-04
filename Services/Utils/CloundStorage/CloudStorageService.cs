using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Application.Utils.CloundStorage
{
    public class CloudStorageService : ICloudStorageService
    {
        private readonly Cloudinary _cloudinary;

        public CloudStorageService(IConfiguration config)
        {
            var account = new Account(
                config["CloudinarySettings:CloudName"],
                config["CloudinarySettings:ApiKey"],
                config["CloudinarySettings:ApiSecret"]
            );

            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Transformation = new Transformation()
                    .Quality("auto")
                    .FetchFormat("auto")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new Exception(result.Error.Message);

            return result.SecureUrl.ToString();
        }

        public async Task<string> UploadImageFromStreamAsync(Stream stream, string fileName)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                Transformation = new Transformation()
                    .Quality("auto")
                    .FetchFormat("auto")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new Exception(result.Error.Message);

            return result.SecureUrl.ToString();
        }

        public async Task DeleteImageAsync(string imageUrl)
        {
            var publicId = GetPublicIdFromUrl(imageUrl);
            Console.WriteLine($"--- ĐANG THỬ XÓA CLOUDINARY VỚI ID: {publicId} ---");

            var deleteParams = new DeletionParams(publicId);

            var result = await _cloudinary.DestroyAsync(deleteParams);

            Console.WriteLine($"--- KẾT QUẢ XÓA: {result.Result} ---");

            if (result.Error != null)
                throw new Exception(result.Error.Message);
        }

        private string GetPublicIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return string.Empty;

            var uri = new Uri(url);
            var pathAfterUpload = uri.AbsolutePath.Split("/upload/")[1];
            var segments = pathAfterUpload.Split('/');
            var filteredSegments = segments.SkipWhile(s => s.StartsWith("v") && s.Length > 1 && char.IsDigit(s[1])).ToList();
            var publicIdWithExtension = string.Join("/", filteredSegments);
            // var segments = uri.AbsolutePath.Split("/upload/")[1];

            return Path.ChangeExtension(publicIdWithExtension, null);
        }
    }
}
