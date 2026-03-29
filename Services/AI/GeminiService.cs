using Repositories.Dto;
using Repositories.Repos.ItemRespos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Services.AI
{
    public class GeminiService: IGeminiService
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
                new {
                    parts = new[] {
                        new { text = $@"Act as a professional Fashion Ontology Expert. 
                            {prompt}
                            
                            ### OUTPUT SCHEMA (STRICT JSON):
                            {{
                              ""item"": ""clothing|footwear|accessory"",
                              ""category"": ""upper_body|lower_body|full_body|footwear|accessory|unknown"",
                              ""sub_category"": ""string"",
                              ""gender"": ""Men|Women|Unisex"",
                              ""main_color"": ""string"",
                              ""sub_color"": ""string"",
                              ""material"": ""string"",
                              ""style"": ""Casual|Formal|Sporty|Streetwear|Vintage|Minimalist"",
                              ""pattern"": ""Solid|Striped|Checked|Floral|Camo|Graphic"",
                              ""sleeve_length"": ""Short|Long|Sleeveless|None"",
                              ""length"": ""Mini|Midi|Maxi|Regular|Cropped"",
                              ""neckline"": ""V-neck|Round|Collar|Hoodie|None"",
                              ""fit"": ""Slim|Regular|Oversized|Loose"",
                              ""item_type"": ""specific product name e.g. jogger, hoodie, loafers"",
                              ""clean_prompt"": ""A high-quality English descriptive caption for SigLIP image-text matching"",
                              ""must_have"": [""essential keywords""],
                              ""must_exclude"": [""forbidden keywords""]
                            }}

                            ### STRATEGIC RULES:
                            1. If context is 'Sporty/Active': exclude [""wool"", ""formal"", ""leather"", ""office"", ""knitted"", ""heels""].
                            2. If context is 'Formal/Office': exclude [""hoodie"", ""sweatpants"", ""camo"", ""distressed""].
                            3. 'clean_prompt' must be in English, focusing on visual attributes.
                            4. ALWAYS return valid JSON. No conversational filler."
                        }
                    }
                }
            },
                    generationConfig = new
                    {
                        temperature = 0.1,
                        response_mime_type = "application/json"
                    }
                };

                var url = $"[https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=](https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=){ApiKey}";

                var response = await _httpClient.PostAsJsonAsync(url, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Gemini API Error: {error}");
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();

                // Truy cập an toàn vào phần tử text
                var messageText = jsonResponse
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text").GetString();

                if (string.IsNullOrEmpty(messageText)) return new SearchIntent { CleanPrompt = prompt };

                // Deserialize trực tiếp từ text
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                var parsedResult = JsonSerializer.Deserialize<SearchIntent>(messageText, options);

                // Đảm bảo CleanPrompt luôn có giá trị để SigLIP không bị lỗi
                if (parsedResult != null && string.IsNullOrEmpty(parsedResult.CleanPrompt))
                    parsedResult.CleanPrompt = prompt;

                return parsedResult ?? new SearchIntent { CleanPrompt = prompt };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- ❌ LỖI CRITICAL GEMINI: {ex.Message} ---");
                return new SearchIntent { CleanPrompt = prompt, Style = "General" };
            }
        }

        public async Task<List<int>> RefineResultsAsync(string userPrompt, string candidatesJson)
        {
            try
            {
                Console.WriteLine("\n=== [GEMINI REFINE START] ===");
                Console.WriteLine($"User Prompt: {userPrompt}");
                Console.WriteLine($"Candidates Sent: {candidatesJson}");

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

                Console.WriteLine($"Gemini Raw Response: {text?.Replace("\n", " ")}");

                var match = Regex.Match(text ?? "[]", @"\[[\d,\s]*\]");
                if (match.Success)
                {
                    var result = JsonSerializer.Deserialize<List<int>>(match.Value);

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
}
