using Microsoft.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shmear.Test
{
    public class ShmearTest
    {
        public DbContextOptions<CardContext> options;

        public ShmearTest()
        {
            var optionsBuilder = new DbContextOptionsBuilder<CardContext>();
            optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString());
            options = optionsBuilder.Options;
        }
    }
}
