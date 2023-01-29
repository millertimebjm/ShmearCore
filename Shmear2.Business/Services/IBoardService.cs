using Shmear2.Business.Database.Models;
using Shmear2.Business.Models;

namespace Shmear2.Business.Services;

public interface IBoardService
{
    Task StartRound(int gameId);
    Task<Board> GetBoardByGameId(int gameId);
    Task<Board> SaveBoard(Board board);
    Task<Board> GetBoard(int boardId);
    Task<bool> DealCards(int gameId);
    Task<GamePlayer> GetNextWagerPlayer(int gameId);
    Task<GamePlayer> GetNextCardPlayer(int gameId, int trickId);
    Task SetWager(int gameId, int playerId, int wager);
    Task<RoundResult> EndRound(int gameId);
    Task<RoundResult> DeterminePointsByTeam(int gameId);
    Card DetermineWinningCard(int gameId, IEnumerable<TrickCard> trickCards);
    int DetermineWinningPlayerId(int gameId, IEnumerable<TrickCard> trickCards);
}