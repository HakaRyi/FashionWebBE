using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Implements.EventAwardingImp
{
    public interface IEventAwardingService
    {
        Task FinalizeAndAwardEventAsync(int eventId);
    }
}
