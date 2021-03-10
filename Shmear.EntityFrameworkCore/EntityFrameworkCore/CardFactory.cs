using Microsoft.EntityFrameworkCore;

namespace Shmear.EntityFramework.EntityFrameworkCore
{
    public static class CardContextFactory
    {
        public static CardContext Create(DbContextOptions<CardContext> options)
        {
            return new CardContext(options);
        }
    }
}
