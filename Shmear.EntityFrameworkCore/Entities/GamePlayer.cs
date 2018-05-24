using System;
using System.Collections.Generic;
using System.Text;

namespace Shmear.EntityFramework.Entities
{
    public class GamePlayer
    {
        public int Id { get; set; }
        public Player Player { get; set; }
        public Game Game { get; set; }
        public int SeatNumber { get; set; }
        public int Wager { get; set; }
        public bool Ready { get; set; }
    }
}
