using Services.Response.EventResp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.Events
{
    public interface IEventService
    {
        Task<bool> CreateEventAsync(int expertId, CreateEventDto dto);

        Task<bool> DepositCoinsAsync(DepositDto dto);

        Task CalculateFinalScoreAsync(int postId, double expertGrade, double communityGrade, double weight);

    }
}
