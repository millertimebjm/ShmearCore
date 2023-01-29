using System;
using System.Collections.Generic;

namespace Shmear2.Business.Database.Models
{
    public class PlayerComputer
    {
        public int Id { get; set; }
        public string Instance { get; set; }
        public string Version { get; set; }
        public int PlayerId { get; set; }

        public Player Player { get; set; }
    }
}
