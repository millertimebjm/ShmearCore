using System;
using System.Collections.Generic;
using System.Text;

namespace Shmear.Business.Models
{
    public class Point
    {
        public int Team { get; set; }
        public PointTypeEnum PointType { get; set; }
        public string OtherData { get; set; }
    }

    public enum PointTypeEnum
    {
        High,
        Low,
        Jack,
        Joker,
        Game,
    }
}
