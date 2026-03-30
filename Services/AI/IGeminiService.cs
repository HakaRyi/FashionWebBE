using Repositories.Dto;

namespace Services.AI
{
    public interface IGeminiService
    {
        Task<SearchIntent> AnalyzePromptAsync(string prompt);

        Task<List<int>> RefineResultsAsync(string userPrompt, string candidatesJson);
    }
}
