using Services.Request.ExpertRatingReq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.ExpertRatingImp
{
    public interface IExpertRatingService
    {
        Task SubmitExpertRatingAsync(ExpertRatingRequest dto);
    }
}
