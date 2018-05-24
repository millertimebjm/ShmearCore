using System;
using System.Collections.Generic;
using System.Text;

namespace Shmear.EntityFramework.Entities
{
    public class TrickCard
    {
        public int Id { get; set; }
        public Trick Trick { get; set; }
        public Player Player { get; set; }
        public Card Card { get; set; }
        public int Sequence { get; set; }
    }
}
