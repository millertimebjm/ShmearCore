using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shmear2.Business.Database;
using Shmear2.Business.Models;
using Shmear2.Business.Database.Models;

namespace Shmear2.Business.Services
{
    public class PlayerComputerService : IPlayerComputerService
    {
        public readonly CardDbContext _cardDb;
        public readonly IShmearService _shmearService;
        public readonly IPlayerService _playerService;
        public PlayerComputerService(
            CardDbContext cardDb,
            IShmearService shmearService,
            IPlayerService playerService)
        {
            _cardDb = cardDb;
            _shmearService = shmearService;
            _playerService = playerService;
        }

        public async Task<int> SetWager(int gameId, int gamePlayerId)
        {
            var highestWager = await _shmearService.GetHighestWager(gameId);
            var randomService = new Random();
            var randomNumber = randomService.Next(100);

            if (highestWager < 2 && randomNumber < 70) return 2; // 70% chance
            if (highestWager < 3 && randomNumber < 20) return 3; // 20% chance
            if (highestWager < 4 && randomNumber < 5) return 4; // 5% chance
            if (highestWager < 5 && randomNumber < 1) return 5; // 1% chance

            return 0;
        }

        public async Task<int> PlayCard(int gameId, int gamePlayerId)
        {
            var gamePlayer = await _shmearService.GetGamePlayer(gamePlayerId);
            var board = await _shmearService.GetBoardByGameId(gameId);
            var hand = (await _shmearService
                .GetHand(gameId, gamePlayer.PlayerId))
                .ToList();
            for (int i = 0; i < hand.Count(); i++)
            {
                var isValidPlay = await _shmearService
                    .ValidCardPlay(gameId, board.Id, gamePlayer.PlayerId, hand[i].CardId);
                if (isValidPlay) return hand[i].CardId;
            }
            throw new Exception("No Valid Play found.");
        }
    }
}
