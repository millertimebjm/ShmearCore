using Microsoft.AspNetCore.SignalR;

namespace Shmear.Web.Hubs
{
    public class ShmearHub : Hub
    {
        const string s = "s";
        private string[] _seats;
        protected static object _seatsLock = new object();

        public ShmearHub()
        {

        }
    }
}
