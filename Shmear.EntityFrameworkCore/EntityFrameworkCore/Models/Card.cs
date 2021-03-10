using System;
using System.Collections.Generic;

namespace Shmear.EntityFramework.EntityFrameworkCore.Models
{
    public partial class Card
    {
        public Card()
        {
            HandCard = new HashSet<HandCard>();
            TrickCard = new HashSet<TrickCard>();
        }

        public int Id { get; set; }
        public int SuitId { get; set; }
        public int ValueId { get; set; }

        public Suit Suit { get; set; }
        public Value Value { get; set; }
        public ICollection<HandCard> HandCard { get; set; }
        public ICollection<TrickCard> TrickCard { get; set; }
    }
}
