using Shmear2.Business.Database.Models;

namespace Shmear2.Business.Services;

public interface ITrickService
{
    Task<IEnumerable<Trick>> GetTricks(int gameId);
    Task<Trick> GetTrick(int trickId);
    Task<Trick> CreateTrick(int gameId);
    Task<Trick> EndTrick(int trickId);
    Task<Trick> PlayCard(int trickId, int playerId, int cardId);
    Task<IEnumerable<TrickCard>> GetAllTrickCards(int gameId);
    void ClearTricks(int gameId);
    Task<IEnumerable<TrickCard>> GetTrickCards(int trickId);
}