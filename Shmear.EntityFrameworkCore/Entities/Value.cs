using System;
using System.Collections.Generic;
using System.Text;

namespace Shmear.EntityFramework.Entities
{
    public class Value
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public char Char { get; set; }
        public int Points { get; set; }
        public int Sequence { get; set; }
    }
}
