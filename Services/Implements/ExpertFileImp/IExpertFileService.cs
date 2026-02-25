using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Services.Response.AccountRep;

namespace Services.Implements.ExpertFileImp
{
    public interface IExpertFileService
    {
        Task<ExpertFileResponse> GetById(int id);
    }
}
