using System;
using System.Collections.Generic;

namespace Shmear2.Business.Database.Models
{
    public class GamePlayer
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int GameId { get; set; }
        public int SeatNumber { get; set; }
        public int? Wager { get; set; }
        public bool Ready { get; set; }

        public Game Game { get; set; }
        public Player Player { get; set; }
    }
}
