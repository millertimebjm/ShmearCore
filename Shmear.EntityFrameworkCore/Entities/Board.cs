using System;

namespace Shmear.EntityFramework.Entities
{
    public class Board
    {
        public int Id { get; set; }
        public Player Dealer { get; set; }
        public Suit TrumpSuit { get; set; }
        public Game Game { get; set; }
        public int Team1Wager { get; set; }
        public int Team2Wager { get; set; }
        public DateTime DateTime { get; set; }
    }
}
