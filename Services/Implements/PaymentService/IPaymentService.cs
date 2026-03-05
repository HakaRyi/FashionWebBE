using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.PaymentService
{
    public interface IPaymentService
    {
        //Task<IEnumerable<PackageDto>> GetPackagesAsync();
        Task<string> CreatePaymentAsync(int accountId, int packageId);
        Task<bool> ProcessPaymentCallbackAsync(string orderCode, bool isSuccess);
    }
}
