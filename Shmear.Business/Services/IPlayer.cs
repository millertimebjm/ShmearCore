using Microsoft.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Shmear.Business.Services
{
    public interface IPlayer
    {
        Task<Player> GetPlayer(DbContextOptions<CardContext> options, int id);
        Task<Player> GetPlayer(DbContextOptions<CardContext> options, string conectionId);
        Task<Player> SavePlayer(DbContextOptions<CardContext> options, Player player);
        Task<Player> GetPlayerByName(DbContextOptions<CardContext> options, string name);
        Task<int> DeletePlayer(DbContextOptions<CardContext> options, int id);
    }
}
