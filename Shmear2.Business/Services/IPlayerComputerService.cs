
namespace Shmear2.Business.Services;

public interface IPlayerComputerService
{
    Task<int> SetWager(int gameId, int gamePlayerId);
    Task<int> PlayCard(int gameId, int GamePlayerId);
}