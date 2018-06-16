using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models
{
    public static class CardFactory
    {
        public static CardContext Create(DbContextOptions<CardContext> optionsBuilder)
        {
            return new CardContext(optionsBuilder);
        }
    }
}
