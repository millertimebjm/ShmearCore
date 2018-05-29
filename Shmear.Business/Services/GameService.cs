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

        //public async static Task<IEnumerable<GamePlayer>> GetGamePlayers(int gameId)
        //{


        //    using (var db = new CardContext())
        //    {
        //        var options = new DataLoadOptions();
        //        options.LoadWith<GamePlayer>(_ => _.Player);
        //        db.LoadOptions = options;

        //        return db.GamePlayers.Where(_ => _.GameId == gameId).ToList();
        //    }
        //}
    }
}
