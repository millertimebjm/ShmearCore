using Shmear2.Business.Database.Models;

namespace Shmear2.Business.Services;

public interface IHandService
{
    Task<bool> AddCard(int gameId, int playerId, int cardId);
    Task<IEnumerable<HandCard>> GetHand(int gameId, int playerId);
}