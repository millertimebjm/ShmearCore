using System;
using System.Collections.Generic;

namespace Shmear2.Business.Database.Models
{
    public class Game
    {
        public Game()
        {
            Board = new HashSet<Board>();
            GamePlayer = new HashSet<GamePlayer>();
            HandCard = new HashSet<HandCard>();
            Trick = new HashSet<Trick>();
        }

        public int Id { get; set; }
        public int Team1Points { get; set; }
        public int Team2Points { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? StartedDate { get; set; }

        public ICollection<Board> Board { get; set; }
        public ICollection<GamePlayer> GamePlayer { get; set; }
        public ICollection<HandCard> HandCard { get; set; }
        public ICollection<Trick> Trick { get; set; }
    }
}
