using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Shmear.Business.Interfaces
{
    public interface IBoardService
    {
        public async static Task StartRound(int gameId);
    }
}
