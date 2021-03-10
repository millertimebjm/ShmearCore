using System;
using System.Collections.Generic;

namespace Shmear.EntityFramework.EntityFrameworkCore.Models
{
    public partial class PlayerComputer
    {
        public int Id { get; set; }
        public string Instance { get; set; }
        public string Version { get; set; }
        public int PlayerId { get; set; }

        public Player Player { get; set; }
    }
}
