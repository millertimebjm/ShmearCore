using System;
using System.Collections.Generic;

namespace Shmear.EntityFramework.EntityFrameworkCore.Models
{
    public partial class Suit
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
