﻿using System;
using System.Collections.Generic;

namespace Shmear2.Business.Database.Models
{
    public class HandCard
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public int PlayerId { get; set; }
        public int CardId { get; set; }

        public Card Card { get; set; }
        public Game Game { get; set; }
        public Player Player { get; set; }
    }
}
