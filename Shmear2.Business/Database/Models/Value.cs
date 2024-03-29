﻿using System;
using System.Collections.Generic;

namespace Shmear2.Business.Database.Models
{
    public class Value
    {
        public Value()
        {
            Card = new HashSet<Card>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Char { get; set; }
        public int Points { get; set; }
        public int Sequence { get; set; }

        public ICollection<Card> Card { get; set; }
    }
}
