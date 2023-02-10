using Shmear2.Business.Database.Models;

namespace Shmear2.Business.Models;

public class Wager
{
    public int GameId { get; set; }
    public int PlayerId { get; set; }
    public GamePlayer? NextGamePlayerId { get; set; }
}
