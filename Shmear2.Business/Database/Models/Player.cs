using System;
using System.Collections.Generic;

namespace Shmear2.Business.Database.Models
{
    public class Player
    {
        public Player()
        {
            Board = new HashSet<Board>();
            GamePlayer = new HashSet<GamePlayer>();
            HandCard = new HashSet<HandCard>();
            PlayerComputer = new HashSet<PlayerComputer>();
            Trick = new HashSet<Trick>();
            TrickCard = new HashSet<TrickCard>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string ConnectionId { get; set; }
        public DateTime KeepAlive { get; set; }
        public bool IsComputer { get; set; } = false;

        public ICollection<Board> Board { get; set; }
        public ICollection<GamePlayer> GamePlayer { get; set; }
        public ICollection<HandCard> HandCard { get; set; }
        public ICollection<PlayerComputer> PlayerComputer { get; set; }
        public ICollection<Trick> Trick { get; set; }
        public ICollection<TrickCard> TrickCard { get; set; }
    }
}
