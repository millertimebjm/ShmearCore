using Shmear2.Business.Models;
using Shmear2.Business.Database.Models;

namespace Shmear2.Business.Services;

public interface IGameService
{
    Task<Game> GetOpenGame();
    Task<Game> CreateGame();
    Task<Game> GetGame(int id);
    Task<IEnumerable<GamePlayer>> GetGamePlayers(int gameId);
    Task<int> AddPlayer(int gameId, int playerId, int seatNumber);
    Task<bool> RemovePlayer(int gameId, int playerId);
    Task<GamePlayer> GetGamePlayer(int gameId, int playerId);
    Task<GamePlayer> GetGamePlayer(int gameId, string connectionId);
    Task<GamePlayer> SaveGamePlayer(GamePlayer gamePlayer);
    Task<bool> StartGame(int gameId);
    Task<IEnumerable<Player>> GetPlayersByGameAsync(int gameId);
    Task<bool> ValidCardPlay(int gameId, int boardId, int playerId, int cardId);
    Task<Game> SaveRoundChange(int gameId, int team1Points, int team2Points);
}