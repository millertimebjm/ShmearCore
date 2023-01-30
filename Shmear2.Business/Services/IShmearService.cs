using Shmear2.Business.Models;
using Shmear2.Business.Database.Models;

namespace Shmear2.Business.Services;

public interface IShmearService
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
    Task<IEnumerable<Card>> GetCards();
    Task<Card> GetCardAsync(int id);
    Card GetCard(int id);
    Card GetCard(SuitEnum suit, ValueEnum value);
    bool SeedSuits();
    bool SeedValues();
    Task<Card> GetCard(int suitId, ValueEnum valueEnum);
    bool SeedCards();
    Task<bool> AddCard(int gameId, int playerId, int cardId);
    Task<IEnumerable<HandCard>> GetHand(int gameId, int playerId);
    Task<IEnumerable<Trick>> GetTricks(int gameId);
    Task<Trick> GetTrick(int trickId);
    Task<Trick> CreateTrick(int gameId);
    Task<Trick> EndTrick(int trickId);
    Task<Trick> PlayCard(int trickId, int playerId, int cardId);
    Task<IEnumerable<TrickCard>> GetAllTrickCards(int gameId);
    void ClearTricks(int gameId);
    Task<IEnumerable<TrickCard>> GetTrickCards(int trickId);
}