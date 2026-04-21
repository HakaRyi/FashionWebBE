using Application.Interfaces;
using Application.Utils.File;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Application.Services
{
    public class GoogleDriveService : IFileService
    {
        private readonly string[] Scopes = { DriveService.Scope.DriveFile };
        private readonly string _folderId = "1LHJwrsLkLxY_358ac4N40eXYG4NjE1yi";
        private readonly IConfiguration _configuration;

        public GoogleDriveService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> UploadAsync(IFormFile file)
        {
            var section = _configuration.GetSection("GoogleDrive");

            // 1. Thiết lập luồng xác thực OAuth 2.0 bằng User Credential
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = section["ClientId"],
                    ClientSecret = section["ClientSecret"]
                },
                Scopes = Scopes
            });

            // 2. Sử dụng Refresh Token để duy trì đăng nhập vĩnh viễn
            var tokenResponse = new TokenResponse
            {
                RefreshToken = section["RefreshToken"]
            };

            var credential = new UserCredential(flow, "user", tokenResponse);

            // 3. Khởi tạo DriveService
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Fashion App"
            });

            // 4. Metadata của file
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = $"{Guid.NewGuid()}_{file.FileName}",
                Parents = new List<string> { _folderId }
            };

            // 5. Thực hiện Upload bằng Stream
            using (var stream = file.OpenReadStream())
            {
                var request = service.Files.Create(fileMetadata, stream, file.ContentType);
                request.Fields = "id, webViewLink";

                var progress = await request.UploadAsync();

                if (progress.Status == Google.Apis.Upload.UploadStatus.Failed)
                {
                    throw new Exception($"Lỗi Drive: {progress.Exception?.Message}");
                }

                return request.ResponseBody.WebViewLink;
            }
        }
    }
}