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
                var gamePlayers = await db.GamePlayer.Where(_ => _.GameId == gameId).ToListAsync();
                foreach (var gamePlayer in gamePlayers)
                {
                    gamePlayer.Player = await db.Player.FindAsync(gamePlayer.PlayerId);
                }
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
                    (await db.Player.SingleAsync(_ => _.Id == playerId)).KeepAlive = DateTime.Now;
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
                var game = await db.Game.Include(_ => _.GamePlayer).SingleAsync(_ => _.Id == gameId);
                var player = await db.Player.SingleAsync(_ => _.Id == playerId);

                if (game.GamePlayer.Any(_ => _.PlayerId == playerId))
                {
                    db.GamePlayer.Remove(await db.GamePlayer.SingleAsync(_ => _.GameId == gameId && _.PlayerId == playerId));
                    await db.SaveChangesAsync();
                }
                return true;
            }
        }
    }
}
