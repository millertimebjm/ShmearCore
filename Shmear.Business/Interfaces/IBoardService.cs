using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Shmear.Business.Interfaces
{
    public interface IBoardService
    {
        Task StartRound(int gameId);
    }
}
