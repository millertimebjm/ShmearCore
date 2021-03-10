using Microsoft.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shmear.Test
{
    public class BaseShmearTest
    {
        public DbContextOptions<CardContext> options;

        public BaseShmearTest()
        {
            var optionsBuilder = new DbContextOptionsBuilder<CardContext>();
            optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
            options = optionsBuilder.Options;
        }

        protected Player GenerateNewPlayer(string name)
        {
            return new Player()
            {
                Id = 0,
                ConnectionId = Guid.NewGuid().ToString(),
                Name = name,
                KeepAlive = DateTime.Now,
            };
        }
    }
}
