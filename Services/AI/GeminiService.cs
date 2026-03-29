using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Services.AI
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "AIzaSyB8uBL_30H-hb4awIewGBft2Eg_UtyHqsU";

        public GeminiService(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<SearchIntent> AnalyzePromptAsync(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    contents = new[] {
                    new { parts = new[] { new { text = $@"
                        Phân tích yêu cầu thời trang: '{prompt}'. 
                        Trả về JSON duy nhất với cấu trúc: 
                        {{
            ""ItemType"": ""t-shirt|shoes|..."",""CleanPrompt"": ""english description"",
            ""MustHave"": [""breathable"", ""polyester"", ""sport""],
            ""MustExclude"": [""wool"", ""sweater"", ""office"", ""formal""]
        }}
                        Luật:
                     1. Nếu là 'đồ thể thao', 'năng động', 'chạy bộ': 
                      MustExclude BẮT BUỘC có: [""wool"", ""sweater"", ""knitted"", ""shirt"", ""formal""]
                      ItemType PHẢI LÀ: ""t-shirt"" hoặc ""pants"" hoặc bất kì từ khóa nào phù hợp với sprot.                        
                    2. CleanPrompt là mô tả chi tiết bằng TIẾNG ANH (ví dụ: 'a professional breathable polo shirt').
                        Chỉ trả về JSON, không giải thích gì thêm."
                    } } }
                }
                };

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={ApiKey}";

                var response = await _httpClient.PostAsJsonAsync(url, requestBody);

                var rawJson = await response.Content.ReadAsStringAsync();

                // 1. Trích xuất text từ cấu trúc phức tạp của Gemini Response
                using var doc = JsonDocument.Parse(rawJson);
                var messageText = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                // 2. Xử lý trường hợp Gemini trả về Markdown (có dấu ```json)
                var cleanJson = Regex.Replace(messageText ?? "{}", @"```json|```", "").Trim();

                // 3. Chuyển thành Object C# (parsedResult)
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var parsedResult = JsonSerializer.Deserialize<SearchIntent>(cleanJson, options);

                return parsedResult ?? new SearchIntent();
            }
            catch (Exception ex)
            {
                // LOG LỖI RA CONSOLE ĐỂ DEBUG
                Console.WriteLine($"--- LỖI GEMINI: {ex.Message} ---");

                // TRẢ VỀ DỮ LIỆU MẶC ĐỊNH ĐỂ APP KHÔNG BỊ CHẾT (FALLBACK)
                return new SearchIntent
                {
                    ItemType = "", // Không lọc theo loại để User vẫn thấy kết quả từ Vector
                    CleanPrompt = prompt,
                    Style = "General"
                };
            }


            //return new SearchIntent
            //{
            //    ItemType = "t-shirt",
            //    CleanPrompt = "sporty t-shirt",
            //    Style = "sporty"
            //};
        }

        public async Task<List<int>> RefineResultsAsync(string userPrompt, string candidatesJson)
        {
            try
            {
                // LOG 1: Xem dữ liệu đầu vào gửi cho Gemini
                Console.WriteLine("\n=== [GEMINI REFINE START] ===");
                Console.WriteLine($"User Prompt: {userPrompt}");
                // Console.WriteLine($"Candidates Sent: {candidatesJson}"); // Bật dòng này nếu muốn xem JSON gửi đi

                var requestBody = new
                {
                    contents = new[] {
                new { parts = new[] { new { text = $@"
                    User want: '{userPrompt}'.
                    Candidate list (JSON): {candidatesJson}

                    TASK:
                    1. Re-rank and Filter: Only keep items that STRICTLY match the 'sporty/active' vibe.
                    2. CRITICAL RULE: If user wants 'sporty/athletic', you MUST EXCLUDE any 'wool', 'sweater', 'knitwear', or 'formal' items.
                    3. Output: A JSON array of IDs only. 
                    
                    Example: [3, 1]
                    DO NOT explain. Just JSON."
                } } }
            }
                };


                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={ApiKey}";
                var response = await _httpClient.PostAsJsonAsync(url, requestBody);
                var rawJson = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(rawJson);
                var text = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

                // LOG 2: Xem phản hồi thô từ Gemini
                Console.WriteLine($"Gemini Raw Response: {text?.Replace("\n", " ")}");

                var match = Regex.Match(text ?? "[]", @"\[[\d,\s]*\]");
                if (match.Success)
                {
                    var result = JsonSerializer.Deserialize<List<int>>(match.Value);

                    // LOG 3: Xem danh sách ID sau khi bóc tách thành công
                    Console.WriteLine($"Refined IDs Found: {string.Join(", ", result)}");
                    Console.WriteLine("=== [GEMINI REFINE END] ===\n");

                    return result;
                }

                Console.WriteLine("!!! [WARNING]: No ID array found in Gemini response.");
                return new List<int>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"!!! [ERROR] Refine: {ex.Message}");
                return new List<int>();
            }
        }
    }

    public class SearchIntent
    {
        public string Category { get; set; } = "";
        public string ItemType { get; set; } = "";
        public string CleanPrompt { get; set; } = "";
        public string Style { get; set; } = "";
        public List<string> MustHave { get; set; } = new();
        public List<string> MustExclude { get; set; } = new();
    }
}
