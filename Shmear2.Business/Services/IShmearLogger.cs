

namespace Shmear2.Business.Services;
public interface IShmearLogger
{
    Task PlayCardLog(int gameId);
    Task EndRoundLog(int gameId);
    Task EndMatchLog(int gameId);
}