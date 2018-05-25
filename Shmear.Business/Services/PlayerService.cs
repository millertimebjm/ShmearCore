using Microsoft.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Shmear.Business.Services
{
    public class PlayerService
    {
        public async static Task<Player> GetPlayer(int id)
        {
            using (var db = new CardContext())
            {
                return await db.Player.SingleAsync(_ => _.Id == id);
            }
        }

        public async static Task<Player> GetPlayer(string conectionId)
        {
            using (var db = new CardContext())
            {
                return await db.Player.SingleOrDefaultAsync(_ => _.ConnectionId == conectionId);
            }
        }

        public async static Task<Player> SavePlayer(Player player)
        {
            using (var db = new CardContext())
            {
                Player returnPlayer;

                if (player.Id == 0)
                {
                    player.KeepAlive = DateTime.Now;
                    await db.Player.AddAsync(player);
                    returnPlayer = player;
                }
                else
                {
                    var playerTemp = new Player();
                    playerTemp = await db.Player.SingleAsync(_ => _.Id == player.Id);
                    playerTemp.ConnectionId = player.ConnectionId;
                    playerTemp.Name = player.Name;
                    playerTemp.KeepAlive = DateTime.Now;
                    returnPlayer = playerTemp;
                }

                await db.SaveChangesAsync();
                return returnPlayer;
            }
        }

        public async static Task<Player> GetPlayerByName(string name)
        {
            using (var db = new CardContext())
            {
                return await db.Player.SingleOrDefaultAsync(_ => _.Name.Equals(name));
            }
        }

        public async static Task<int> DeletePlayer(int id)
        {
            using (var db = new CardContext())
            {
                var player = await db.Player.SingleAsync(_ => _.Id == id);
                db.Player.Remove(player);
                return await db.SaveChangesAsync();
            }
        }
    }
}
