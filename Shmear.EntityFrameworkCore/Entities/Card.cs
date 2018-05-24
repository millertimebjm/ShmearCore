using System;
using System.Collections.Generic;
using System.Text;

namespace Shmear.EntityFramework.Entities
{
    public class Card
    {
        public int Id { get; set; }
        public Suit Suit { get; set; }
        public Value Value { get; set; }
    }
}
