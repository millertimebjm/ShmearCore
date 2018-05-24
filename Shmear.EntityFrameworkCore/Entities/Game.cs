using System;
using System.Collections.Generic;
using System.Text;

namespace Shmear.EntityFramework.Entities
{
    public class Game
    {
        public int Id { get; set; }
        public int Team1Points { get; set; }
        public int Team2Points { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime StartedDate { get; set; }

        public ICollection<Board> Boards { get; set; }
        public ICollection<GamePlayer> GamePlayers { get; set; }
    }
}
