﻿using System;
using System.Collections.Generic;

namespace Shmear.EntityFramework.EntityFrameworkCore.Models
{
    public partial class Board
    {
        public int Id { get; set; }
        public int? DealerPlayerId { get; set; }
        public int? TrumpSuitId { get; set; }
        public int GameId { get; set; }
        public int? Team1Wager { get; set; }
        public int? Team2Wager { get; set; }
        public DateTime DateTime { get; set; }

        public Player DealerPlayer { get; set; }
        public Game Game { get; set; }
        public Suit TrumpSuit { get; set; }
    }
}
