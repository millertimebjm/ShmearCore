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
