using Microsoft.EntityFrameworkCore;
using Shmear.Business.Services;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Shmear.Test
{
    public class CardTest : BaseShmearTest
    {
        public CardTest() : base()
        {

        }

        [Fact]
        public void SeedTest()
        {
            var seedDatabase = new SeedDatabase();
            seedDatabase.RunWithOptions(options);
        }
    }
}
