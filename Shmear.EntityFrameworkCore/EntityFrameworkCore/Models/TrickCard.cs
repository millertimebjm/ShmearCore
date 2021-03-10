using System;
using System.Collections.Generic;

namespace Shmear.EntityFramework.EntityFrameworkCore.Models
{
    public partial class TrickCard
    {
        public int Id { get; set; }
        public int TrickId { get; set; }
        public int PlayerId { get; set; }
        public int CardId { get; set; }
        public int Sequence { get; set; }

        public Card Card { get; set; }
        public Player Player { get; set; }
        public Trick Trick { get; set; }
    }
}
