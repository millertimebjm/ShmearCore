using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Shmear2.Business.Database;
using Shmear2.Business.Models;
using Shmear2.Business.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Shmear2.Business.Services
{
    public class GameService : IGameService
    {
        private readonly CardDbContext _cardDb;
        private readonly ITrickService _trickService;
        private readonly ICardService _cardService;
        private readonly IHandService _handService;
        private readonly IBoardService _boardService;
        public GameService(
            CardDbContext cardDb,
            ITrickService trickService,
            ICardService cardService,
            IHandService handService,
            IBoardService boardService)
        {
            _cardDb = cardDb;
            _trickService = trickService;
            _cardService = cardService;
            _handService = handService;
            _boardService = boardService;
        }

        public async Task<Game> GetOpenGame()
        {
            var openGames = await _cardDb
                .Game
                .Where(_ => _.StartedDate == null && _.GamePlayer.Count < 4)
                .ToListAsync();
            if (openGames.Any())
            {
                return openGames.First();
            }

            return await CreateGame();
        }

        public async Task<Game> CreateGame()
        {
            var game = new Game
            {
                Id = 0,
                CreatedDate = DateTime.Now,
                StartedDate = null
            };

            await _cardDb.Game.AddAsync(game);
            await _cardDb.SaveChangesAsync();

            return await GetGame(game.Id);
        }

        public async Task<Game> GetGame(int id)
        {
            return await _cardDb.Game.SingleOrDefaultAsync(_ => _.Id == id);
        }

        public async Task<IEnumerable<GamePlayer>> GetGamePlayers(int gameId)
        {
            return await _cardDb.GamePlayer.Include(p => p.Player).Where(_ => _.GameId == gameId).ToListAsync();
        }

        public async Task<int> AddPlayer(int gameId, int playerId, int seatNumber)
        {
            var game = await _cardDb.Game.SingleAsync(_ => _.Id == gameId);
            var player = await _cardDb.Player.SingleAsync(_ => _.Id == playerId);
            var gamePlayers = game.GamePlayer.OrderBy(_ => _.SeatNumber);

            if (!gamePlayers.Any(_ => _.SeatNumber == seatNumber))
            {
                var gamePlayer = new GamePlayer()
                {
                    GameId = gameId,
                    PlayerId = playerId,
                    SeatNumber = seatNumber,
                    Ready = false,
                };

                if (game.GamePlayer.Any(_ => _.PlayerId == playerId))
                {
                    var gamePlayerToRemove = await _cardDb.GamePlayer.SingleAsync(_ => _.GameId == gameId && _.PlayerId == playerId);
                    _cardDb.GamePlayer.Remove(gamePlayerToRemove);
                }

                await _cardDb.GamePlayer.AddAsync(gamePlayer);
                player.KeepAlive = DateTime.Now;
                return await _cardDb.SaveChangesAsync();
            }

            return 0;
        }

        public async Task<bool> RemovePlayer(int gameId, int playerId)
        {
            var gamePlayer = await _cardDb
                .GamePlayer
                .SingleAsync(_ => _.GameId == gameId && _.PlayerId == playerId);
            _cardDb.GamePlayer.Remove(gamePlayer);
            await _cardDb.SaveChangesAsync();
            return true;
        }

        public async Task<GamePlayer> GetGamePlayer(int gameId, int playerId)
        {
            return await _cardDb
                .GamePlayer
                .Include(_ => _.Player)
                .SingleAsync(_ => _.GameId == gameId && _.PlayerId == playerId);
        }

        public async Task<GamePlayer> GetGamePlayer(int gameId, string connectionId)
        {
            return await _cardDb
                .GamePlayer
                .SingleAsync(_ => _.GameId == gameId && _.Player.ConnectionId == connectionId);
        }

        public async Task<GamePlayer> SaveGamePlayer(GamePlayer gamePlayer)
        {
            GamePlayer gamePlayerReturn;
            if (gamePlayer.Id == 0)
            {
                await _cardDb.GamePlayer.AddAsync(gamePlayer);
                gamePlayerReturn = gamePlayer;
            }
            else
            {
                var gamePlayerTemp = await _cardDb.GamePlayer.SingleAsync(_ => _.Id == gamePlayer.Id);
                gamePlayerTemp.SeatNumber = gamePlayer.SeatNumber;
                gamePlayerTemp.Wager = gamePlayer.Wager;
                gamePlayerTemp.Ready = gamePlayer.Ready;
                gamePlayerReturn = gamePlayerTemp;
            }
            await _cardDb.SaveChangesAsync();
            return await GetGamePlayer(gamePlayerReturn.GameId, gamePlayerReturn.PlayerId);
        }

        public async Task<bool> StartGame(int gameId)
        {
            var game = await _cardDb.Game.SingleAsync(_ => _.Id == gameId);
            if (game.StartedDate == null)
            {
                game.StartedDate = DateTime.Now;
                await _cardDb.SaveChangesAsync();
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<IEnumerable<Player>> GetPlayersByGameAsync(int gameId)
        {
            return (await GetGamePlayers(gameId)).Select(_ => _.Player);
        }

        public async Task<bool> ValidCardPlay(int gameId, int boardId, int playerId, int cardId)
        {
            var gamePlayer = await _cardDb.GamePlayer.Include(p => p.Player).SingleAsync(_ => _.GameId == gameId && _.PlayerId == playerId);
            var player = gamePlayer.Player;

            var cards = await _handService.GetHand(gameId, player.Id);
            var tricks = await _trickService.GetTricks(gameId);
            var trick = tricks.SingleOrDefault(_ => _.CompletedDate == null);
            if (trick == null || trick.Id == 0)
            {
                trick = await _trickService.CreateTrick(gameId);
            }

            var board = await _boardService.GetBoard(boardId);
            var trumpSuitId = board.TrumpSuitId ?? 0;
            var card = await _cardService.GetCardAsync(cardId);

            if (trumpSuitId == 0)
            {
                if (card.Value.Name == ValueEnum.Joker.ToString())
                    return false;

                return true;
            }
            else
            {
                var trickCards = await _trickService.GetTrickCards(trick.Id);
                if (trickCards.Any())
                {
                    var cardLed = (await _cardService.GetCardAsync(trickCards.First().CardId));
                    var suitLedId = cardLed.SuitId;
                    if (card.SuitId == trumpSuitId)
                        return true;
                    if (card.Value.Name == ValueEnum.Joker.ToString())
                        return true;
                    if (suitLedId == card.SuitId)
                        return true;
                    if (cards.All(_ => _cardService.GetCard(_.Card.Id).SuitId != suitLedId)
                        && cards.All(_ => _cardService.GetCard(_.Card.Id).Value.Name != ValueEnum.Joker.ToString()))
                        return true;

                    return false;
                }
                else
                    return true;
            }
        }

        public async Task<Game> SaveRoundChange(int gameId, int team1Points, int team2Points)
        {
            var game = await _cardDb.Game.SingleAsync(_ => _.Id == gameId);
            game.Team1Points = team1Points;
            game.Team2Points = team2Points;
            await _cardDb.SaveChangesAsync();
            return game;
        }
    }
}
