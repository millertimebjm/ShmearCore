using System;
using System.Collections.Generic;
using System.Text;

namespace Shmear.EntityFramework.Entities
{
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ConnectionId { get; set; }
        public DateTime KeepAlive { get; set; }
    }
}
