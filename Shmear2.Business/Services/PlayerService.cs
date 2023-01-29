using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Shmear2.Business.Database;
using Shmear2.Business.Models;
using Shmear2.Business.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Shmear2.Business.Services
{
    public class PlayerService : IPlayerService
    {
        private readonly CardDbContext _cardDb;
        public PlayerService(CardDbContext cardDb)
        {
            _cardDb = cardDb;
        }

        public async Task<Player> GetPlayer(int id)
        {
            return await _cardDb.Player.SingleOrDefaultAsync(_ => _.Id == id);
        }

        public async Task<Player> GetPlayer(string conectionId)
        {
            return await _cardDb.Player.SingleOrDefaultAsync(_ => _.ConnectionId == conectionId);
        }

        public async Task<Player> SavePlayer(Player player)
        {
            Player returnPlayer;

            if (player.Id == 0)
            {
                player.KeepAlive = DateTime.Now;
                await _cardDb.Player.AddAsync(player);
                returnPlayer = player;
            }
            else
            {
                Player playerTemp;
                playerTemp = await _cardDb.Player.SingleAsync(_ => _.Id == player.Id);
                playerTemp.ConnectionId = player.ConnectionId;
                playerTemp.Name = player.Name;
                playerTemp.KeepAlive = DateTime.Now;
                returnPlayer = playerTemp;
            }

            await _cardDb.SaveChangesAsync();
            return returnPlayer;
        }

        public async Task<Player> GetPlayerByName(string name)
        {
            return await _cardDb.Player.SingleOrDefaultAsync(_ => _.Name.Equals(name));
        }

        public async Task<int> DeletePlayer(int id)
        {
            var player = await _cardDb.Player.SingleAsync(_ => _.Id == id);
            _cardDb.Player.Remove(player);
            return await _cardDb.SaveChangesAsync();
        }
    }
}
