using System;
using System.Collections.Generic;
using System.Text;

namespace Shmear.EntityFramework.Entities
{
    public class Trick
    {
        public int Id { get; set; }
        public Game Game { get; set; }
        public Player WinningPlayer { get; set; }
        public int Sequence { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime CompletionDate { get; set; }
    }
}
