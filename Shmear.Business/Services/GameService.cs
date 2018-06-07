using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
//using Shmear.EntityFramework.EntityFrameworkCore.SqlServer;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Shmear.Business.Services
{
    public class GameService
    {
        public async static Task<Game> GetOpenGame()
        {
            
            using (var db = new CardContext())
            {
                var openGames = await db.Game.Where(_ => _.CreatedDate != null && (_.StartedDate == null && _.GamePlayer.Count() < 4)).ToListAsync();
                if (openGames.Any())
                {
                    return openGames.First();
                }

                return await CreateGame();
            }
        }

        public async static Task<Game> CreateGame()
        {
            var game = new EntityFramework.EntityFrameworkCore.SqlServer.Models.Game
            {
                Id = 0,
                CreatedDate = DateTime.Now,
                StartedDate = null
            };

            using (var db = new CardContext())
            {
                await db.Game.AddAsync(game);
                await db.SaveChangesAsync();
            }
            return await GetGame(game.Id);
        }

        public async static Task<Game> GetGame(int id)
        {
            using (var db = new CardContext())
            {
                return await db.Game.SingleAsync(_ => _.Id == id);
            }
        }

        public async static Task<IEnumerable<GamePlayer>> GetGamePlayers(int gameId)
        {
            using (var db = new CardContext())
            {
                var gamePlayers = await db.GamePlayer.Include(_ => _.Player).Where(_ => _.GameId == gameId).ToListAsync();
                //foreach (var gamePlayer in gamePlayers)
                //{
                //    gamePlayer.Player = await db.Player.FindAsync(gamePlayer.PlayerId);
                //}
                return gamePlayers;
            }
        }

        public async static Task<bool> AddPlayer(int gameId, int playerId, int seatNumber)
        {
            using (var db = new CardContext())
            {
                var game = await db.Game.SingleAsync(_ => _.Id == gameId);
                var player = await db.Player.SingleAsync(_ => _.Id == playerId);
                var gamePlayers = game.GamePlayer.OrderBy(_ => _.SeatNumber);

                if (!gamePlayers.Any(_ => _.SeatNumber == seatNumber))
                {
                    var gamePlayer = new GamePlayer()
                    {
                        GameId = gameId,
                        PlayerId = playerId,
                        SeatNumber = seatNumber,
                        Ready = false
                    };

                    if (game.GamePlayer.Any(_ => _.PlayerId == playerId))
                    {
                        var gamePlayerToRemove = await db.GamePlayer.SingleAsync(_ => _.GameId == gameId && _.PlayerId == playerId);
                        db.GamePlayer.Remove(gamePlayerToRemove);
                    }

                    await db.GamePlayer.AddAsync(gamePlayer);
                    player.KeepAlive = DateTime.Now;
                    await db.SaveChangesAsync();
                    return true;
                }

                return false;
            }
        }

        public async static Task<bool> RemovePlayer(int gameId, int playerId)
        {
            using (var db = new CardContext())
            {
                //var game = await db.Game.Include(_ => _.GamePlayer).SingleAsync(_ => _.Id == gameId);
                //var player = await db.Player.SingleAsync(_ => _.Id == playerId);

                //if (game.GamePlayer.Any(_ => _.PlayerId == playerId))
                //{
                //    db.GamePlayer.Remove(await db.GamePlayer.SingleAsync(_ => _.GameId == gameId && _.PlayerId == playerId));
                //    await db.SaveChangesAsync();
                //}

                var gamePlayer = await db.GamePlayer.SingleAsync(_ => _.GameId == gameId && _.PlayerId == playerId);
                db.GamePlayer.Remove(gamePlayer);
                await db.SaveChangesAsync();
                return true;
            }
        }

        public async static Task<GamePlayer> GetGamePlayer(int gameId, int playerId)
        {
            using (var db = new CardContext())
            {
                //var game = await db.Game.SingleAsync(_ => _.Id == gameId);
                return await db.GamePlayer.Include(_ => _.Player).SingleAsync(_ => _.GameId == gameId && _.PlayerId == playerId);
            }
        }

        public async static Task<GamePlayer> GetGamePlayer(int gameId, string connectionId)
        {
            using (var db = new CardContext())
            {
                var game = await db.Game.SingleAsync(_ => _.Id == gameId);
                return await db.GamePlayer.SingleAsync(_ => _.GameId == gameId && _.Player.ConnectionId == connectionId);
            }
        }

        public async static Task<GamePlayer> SaveGamePlayer(GamePlayer gamePlayer)
        {
            using (var db = new CardContext())
            {
                var gamePlayerReturn = new GamePlayer();
                if (gamePlayer.Id == 0)
                {
                    await db.GamePlayer.AddAsync(gamePlayer);
                    gamePlayerReturn = gamePlayer;
                }
                else
                {
                    var gamePlayerTemp = await db.GamePlayer.SingleAsync(_ => _.Id == gamePlayer.Id);
                    gamePlayerTemp.SeatNumber = gamePlayer.SeatNumber;
                    gamePlayerTemp.Wager = gamePlayer.Wager;
                    gamePlayerTemp.Ready = gamePlayer.Ready;
                    gamePlayerReturn = gamePlayerTemp;
                }
                await db.SaveChangesAsync();
                return await GameService.GetGamePlayer(gamePlayerReturn.GameId, gamePlayerReturn.PlayerId);
            }
        }

        public async static Task<bool> StartGame(int gameId)
        {
            using (var db = new CardContext())
            {

                var game = await db.Game.SingleAsync(_ => _.Id == gameId);
                if (game.StartedDate == null)
                {
                    game.StartedDate = DateTime.Now;
                    await db.SaveChangesAsync();
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static async Task<List<Player>> GetPlayersByGameAsync(int gameId)
        {
            using (var db = new CardContext())
            {
                return await db.GamePlayer.Where(_ => _.GameId == gameId).Select(_ => _.Player).ToListAsync();
            }
        }

        public static async Task<IEnumerable<Player>> GetPlayersByGame(int gameId)
        {
            using (var db = new CardContext())
            {
                return await db.GamePlayer.Where(_ => _.GameId == gameId).Select(_ => _.Player).ToListAsync();
            }
        }

        public static async Task<bool> ValidCardPlay(int gameId, int boardId, int playerId, int cardId)
        {
            using (var db = new CardContext())
            {
                var gamePlayer = await db.GamePlayer.Include(_ => _.Player).SingleAsync(_ => _.GameId == gameId && _.PlayerId == playerId);
                var player = gamePlayer.Player;

                //var game = await db.Game.SingleAsync(_ => _.Id == gameId);
                var cards = await HandService.GetHand(gameId, player.Id);
                var tricks = await TrickService.GetTricks(gameId);
                var trick = tricks.SingleOrDefault(_ => _.CompletedDate == null);
                if (trick == null || trick.Id == 0)
                {
                    trick = await TrickService.CreateTrick(gameId);
                }

                var board = await BoardService.GetBoard(boardId);
                var trumpSuitId = board.TrumpSuitId ?? 0;
                var card = await CardService.GetCardAsync(cardId);

                //if (!cards.Select(_ => _.CardId).Contains(card.Id))
                //    throw new Exception("Player does not have that card in their hand.");

                if (trumpSuitId == 0)
                {
                    if (card.Value.Name == CardService.ValueEnum.Joker.ToString())
                        return false;

                    return true;
                }
                else
                {
                    if (trick.TrickCard.Any())
                    {
                        var cardLed = (await CardService.GetCardAsync(trick.TrickCard.First().CardId));
                        var suitLedId = cardLed.SuitId;
                        if (card.SuitId == trumpSuitId)
                            return true;
                        if (card.Value.Name == CardService.ValueEnum.Joker.ToString())
                            return true;
                        if (suitLedId == card.SuitId)
                            return true;
                        if (cards.All(_ => CardService.GetCard(_.Card.Id).SuitId != suitLedId)
                            && cards.All(_ => CardService.GetCard(_.Card.Id).Value.Name != CardService.ValueEnum.Joker.ToString()))
                            return true;

                        return false;
                    }
                    else
                        return true;
                }
            }
        }

        public static async Task<Game> SaveGame(Game game)
        {
            using (var db = new CardContext())
            {
                if (game.Id == 0)
                {
                    game.Team1Points = 0;
                    game.Team2Points = 0;
                    game.CreatedDate = DateTime.Now;
                    game.StartedDate = null;
                    await db.Game.AddAsync(game);
                }
                else
                {
                    var gameTemp = await db.Game.SingleAsync(_ => _.Id == game.Id);
                    gameTemp.Team1Points = game.Team1Points;
                    gameTemp.Team2Points = game.Team2Points;
                }

                await db.SaveChangesAsync();
            }
            return game;
        }
    }
}
