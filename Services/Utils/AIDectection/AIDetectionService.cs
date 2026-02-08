using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Utils.AIDectection
{
    public class AIDetectionService : IAIDetectionService
    {
        private readonly string _pythonPath;
        private readonly string _scriptPath;

        public AIDetectionService(IConfiguration config)
        {
            _pythonPath = config["AISettings:PythonPath"]!;
            _scriptPath = config["AISettings:ScriptPath"]!;
        }

        public async Task<bool> DetectFashionItemsAsync(string imageUrl)
        {
            // Cấu hình tiến trình chạy Python
            var start = new ProcessStartInfo
            {
                FileName = _pythonPath,
                // Truyền đường dẫn script và URL ảnh làm tham số
                Arguments = $"\"{_scriptPath}\" \"{imageUrl}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            return await Task.Run(() =>
            {
                using var process = Process.Start(start);
                if (process == null) return false;

                // Đọc kết quả từ lệnh print("True") hoặc print("False") trong Python
                using var reader = process.StandardOutput;
                string result = reader.ReadToEnd().Trim(); // Xóa khoảng trắng/xuống dòng thừa

                // Đọc lỗi nếu có (để debug)
                string error = process.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine("Python Error: " + error);
                }

                process.WaitForExit();

                // So sánh chuỗi kết quả
                return result.Equals("True", StringComparison.OrdinalIgnoreCase);
            });
        }
    }
}
