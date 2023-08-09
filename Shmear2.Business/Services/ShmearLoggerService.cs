using Shmear2.Business.Database;

namespace Shmear2.Business.Services;

public class ShmearLoggerService : IShmearLogger
{
    public readonly IShmearService _shmearService;
    public ShmearLoggerService(IShmearService shmearService)
    {
        _shmearService = shmearService;
    }

    public Task SetWagerLog(int gameId)
    {
        throw new NotImplementedException();
    }

    public Task PlayCardLog(int gameId)
    {
        // var gameTask = _shmearService.GetGame(gameId);
        // var gamePlayersTask = _shmearService.GetGamePlayers(gameId);
        // var trickTask = _shmearService.GetLatestNonEmptyTrick(gameId);
        // var task = Task.WhenAll(gameTask, gamePlayersTask, trickTask);
        // var asdf = new
        // {
        //     gameTask.Result.StartedDate,
        //     gameTask.Result.Team1Matches,
        //     gameTask.Result.Team1Points,
        //     gameTask.Result.Team2Matches,
        //     gameTask.Result.Team2Points,
        //     gamePlayersTask.Result[0].
        //     };

        throw new NotImplementedException();
    }

    public Task EndRoundLog(int gameId)
    {
        throw new NotImplementedException();
    }

    public Task EndMatchLog(int gameId)
    {
        throw new NotImplementedException();
    }
}