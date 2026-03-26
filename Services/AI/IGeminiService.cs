using Repositories.Dto;
using Repositories.Repos.ItemRespos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.AI
{
    public interface IGeminiService
    {
        Task<SearchIntent> AnalyzePromptAsync(string prompt);

        Task<List<int>> RefineResultsAsync(string userPrompt, string candidatesJson);
    }
}
