using System;
using System.Collections.Generic;

namespace Shmear2.Business.Database.Models
{
    public class Trick
    {
        public Trick()
        {
            TrickCard = new HashSet<TrickCard>();
        }

        public int Id { get; set; }
        public int GameId { get; set; }
        public int? WinningPlayerId { get; set; }
        public int Sequence { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        public Game Game { get; set; }
        public Player WinningPlayer { get; set; }
        public ICollection<TrickCard> TrickCard { get; set; }
    }
}
