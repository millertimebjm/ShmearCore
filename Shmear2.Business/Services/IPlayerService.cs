using Shmear2.Business.Database.Models;

namespace Shmear2.Business.Services;

public interface IPlayerService
{
    Task<Player> GetPlayer(int id);
    Task<Player> GetPlayer(string conectionId);
    Task<Player> SavePlayer(Player player);
    Task<Player> GetPlayerByName(string name);
    Task<int> DeletePlayer(int id);
}