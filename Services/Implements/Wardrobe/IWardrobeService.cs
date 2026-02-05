using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Services.Request.WardrobeReq;
using Services.Response.WardrobeResp;

namespace Services.Implements.Wardrobe
{
    public interface IWardrobeService
    {
        Task<List<WardrobeResponse>> GetAll();
        Task<WardrobeResponse> GetById(int id);
        Task<int> Create(WardrobeRequest request);

    }
}
