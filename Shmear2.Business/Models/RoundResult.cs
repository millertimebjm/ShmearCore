using System;
using System.Collections.Generic;
using System.Text;

namespace Shmear2.Business.Models
{
    public class RoundResult
    {
        public int Team1RoundChange { get; set; }
        public int Team2RoundChange { get; set; }
        public IEnumerable<Point> Team1Points { get; set; }
        public IEnumerable<Point> Team2Points { get; set; }
        public bool Bust { get; set; }
        public int TeamWager { get; set; }
        public int TeamWagerValue { get; set; }

        public RoundResult()
        {
            Team1RoundChange = 0;
            Team2RoundChange = 0;
            Bust = false;
            TeamWager = 0;
            TeamWagerValue = 0;
        }
    }
}
