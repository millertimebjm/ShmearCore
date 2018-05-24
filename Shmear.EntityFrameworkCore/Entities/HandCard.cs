using System;
using System.Collections.Generic;
using System.Text;

namespace Shmear.EntityFramework.Entities
{
    public class HandCard
    {
        public int Id { get; set; }
        public Game Game { get; set; }
        public Player Player { get; set; }
        public Card Card { get; set; }
    }
}
