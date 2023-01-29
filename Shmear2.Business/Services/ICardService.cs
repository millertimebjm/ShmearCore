using Shmear2.Business.Models;
using Shmear2.Business.Database.Models;

namespace Shmear2.Business.Services;

public interface ICardService
{
    Task<IEnumerable<Card>> GetCards();
    Task<Card> GetCardAsync(int id);
    Card GetCard(int id);
    Card GetCard(SuitEnum suit, ValueEnum value);
    bool SeedSuits();
    bool SeedValues();
    Task<Card> GetCard(int suitId, ValueEnum valueEnum);
    bool SeedCards();
}