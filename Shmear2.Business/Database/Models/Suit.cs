using System;
using System.Collections.Generic;

namespace Shmear2.Business.Database.Models
{
    public class Suit
    {
        public Suit()
        {
            Board = new HashSet<Board>();
            Card = new HashSet<Card>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Char { get; set; }

        public ICollection<Board> Board { get; set; }
        public ICollection<Card> Card { get; set; }
    }
}
