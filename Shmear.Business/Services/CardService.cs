using Microsoft.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shmear.Business.Services
{
    public class CardService
    {
        public enum SuitEnum
        {
            None,
            Clubs,
            Spades,
            Diamonds,
            Hearts
        }

        public enum ValueEnum
        {
            Seven = 7,
            Eight = 8,
            Nine = 9,
            Ten = 10,
            Joker = 11,
            Jack = 12,
            Queen = 13,
            King = 14,
            Ace = 15
        }

        public static async Task<IEnumerable<Card>> GetCards(DbContextOptions<CardContext> options)
        {
            using (var db = CardContextFactory.Create(options))
            {
                return await db.Card.ToListAsync();
            }
        }

        public static async Task<Card> GetCardAsync(DbContextOptions<CardContext> options, int id)
        {
            using (var db = CardContextFactory.Create(options))
            {
                return await db.Card.Include(s => s.Suit).Include(_ => _.Value).SingleAsync(_ => _.Id == id);
            }
        }

        public static Card GetCard(DbContextOptions<CardContext> options, int id)
        {
            using (var db = CardContextFactory.Create(options))
            {
                return db.Card.Include(s => s.Suit).Include(_ => _.Value).Single(_ => _.Id == id);
            }
        }

        public static Card GetCard(DbContextOptions<CardContext> options, SuitEnum suit, ValueEnum value)
        {
            using (var db = CardContextFactory.Create(options))
            {
                return db.Card.Single(_ => _.Suit.Name == suit.ToString() && _.Value.Name == value.ToString());
            }
        }

        public static bool SeedSuits(DbContextOptions<CardContext> options)
        {
            var suits = new List<Suit>()
            {
                new Suit()
                {
                    Name = "Clubs",
                    Char = "♣"
                },
                new Suit()
                {
                    Name = "Spades",
                    Char = "♠"
                },
                new Suit()
                {
                    Name = "Diamonds",
                    Char = "♦"
                },
                new Suit()
                {
                    Name = "Hearts",
                    Char = "♥"
                },
                new Suit()
                {
                    Name = "None",
                    Char = " "
                }
            };

            using (var db = CardContextFactory.Create(options))
            {
                foreach (var suit in suits)
                {
                    if (!db.Suit.Any(_ => _.Name.Equals(suit.Name)))
                    {
                        db.Suit.Add(suit);
                        db.SaveChanges();
                    }
                }
            }
            return true;
        }

        public static bool SeedValues(DbContextOptions<CardContext> options)
        {
            var values = new List<Value>()
            {
                new Value()
                {
                    Name = "Seven",
                    Char = "7",
                    Points = 0,
                    Sequence = 10
                },
                new Value()
                {
                    Name = "Eight",
                    Char = "8",
                    Sequence = 20
                },
                new Value()
                {
                    Name = "Nine",
                    Char = "9",
                    Sequence = 30
                },
                new Value()
                {
                    Name = "Ten",
                    Char = "T",
                    Sequence = 40
                },
                new Value()
                {
                    Name = "Joker",
                    Char = "J",
                    Sequence = 50
                },
                new Value()
                {
                    Name = "Jack",
                    Char = "J",
                    Sequence = 60
                },
                new Value()
                {
                    Name = "Queen",
                    Char = "Q",
                    Sequence = 70
                },
                new Value()
                {
                    Name = "King",
                    Char = "K",
                    Sequence = 80
                },
                new Value()
                {
                    Name = "Ace",
                    Char = "A",
                    Sequence = 90
                },
            };

            using (var db = CardContextFactory.Create(options))
            {
                foreach (var value in values)
                {
                    if (!db.Value.Any(_ => _.Name.Equals(value.Name)))
                    {
                        db.Value.Add(value);
                        db.SaveChanges();
                    }
                }
            }
            return true;
        }

        public async static Task<Card> GetCard(DbContextOptions<CardContext> options, int suitId, ValueEnum valueEnum)
        {
            using (var db = CardContextFactory.Create(options))
            {
                return await db.Card.Include(s => s.Suit).Include(_ => _.Value).SingleAsync(_ => _.SuitId == suitId && _.Value.Name == valueEnum.ToString());
            }
        }

        public static bool SeedCards(DbContextOptions<CardContext> options)
        {
            using (var db = CardContextFactory.Create(options))
            {
                foreach (SuitEnum suit in Enum.GetValues(typeof(SuitEnum)))
                {
                    if (suit != SuitEnum.None)
                    {
                        foreach (ValueEnum value in Enum.GetValues(typeof(ValueEnum)))
                        {
                            if (value != ValueEnum.Joker)
                            {
                                var suitId = db.Suit.Single(_ => _.Name.Equals(suit.ToString())).Id;
                                var valueId = db.Value.Single(_ => _.Name.Equals(value.ToString())).Id;
                                if (!db.Card.Any(_ => _.SuitId == suitId && _.ValueId == valueId))
                                {
                                    var card = new Card()
                                    {
                                        SuitId = suitId,
                                        ValueId = valueId
                                    };
                                    db.Card.Add(card);
                                    db.SaveChanges();
                                }
                            }
                        }
                    }
                }
                var noneSuitId = db.Suit.Single(_ => _.Name.Equals(SuitEnum.None.ToString())).Id;
                var jokerValueId = db.Value.Single(_ => _.Name.Equals(ValueEnum.Joker.ToString())).Id;
                if (!db.Card.Any(_ => _.SuitId == noneSuitId && _.ValueId == jokerValueId))
                {
                    var joker = new Card()
                    {
                        SuitId = noneSuitId,
                        ValueId = jokerValueId
                    };

                    db.Card.Add(joker);
                    db.SaveChanges();
                }
            }

            return true;
        }
    }
}
