using Microsoft.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Shmear.Business.Services
{
    public static class PlayerFactory
    {
        //public async static Task<IPlayer> GetPlayer(DbContextOptions<CardContext> options, int playerId)
        //{
        //    using (var db = CardContextFactory.Create(options))
        //    {
        //        var player = await db.Player.SingleAsync(_ => _.Id == playerId);
        //        return player;
        //    }
        //}
    }
}
