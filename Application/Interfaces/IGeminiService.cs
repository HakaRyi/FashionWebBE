using Domain.Dto;

namespace Application.Interfaces
{
    public interface IGeminiService
    {
        Task<SearchIntent> AnalyzePromptAsync(string prompt);

        Task<List<int>> RefineResultsAsync(string userPrompt, string candidatesJson);
    }
}
