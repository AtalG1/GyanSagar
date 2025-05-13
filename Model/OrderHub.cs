using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace GyanSagarNew.Model
{
    public class OrderHub : Hub
    {

        public async Task SendOrderUpdate(string message)
        {
            await Clients.All.SendAsync("ReceiveOrderUpdate", message);
        }

    }
}
