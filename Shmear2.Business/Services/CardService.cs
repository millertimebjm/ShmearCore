using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shmear2.Business.Database;
using Shmear2.Business.Models;
using Shmear2.Business.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Shmear2.Business.Services
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

    public class CardService : ICardService
    {
        private readonly CardDbContext _cardDb;
        public CardService(CardDbContext cardDb)
        {
            _cardDb = cardDb;
        }

        public async Task<IEnumerable<Card>> GetCards()
        {
            return await _cardDb
                .Card
                .ToListAsync();
        }

        public async Task<Card> GetCardAsync(int id)
        {
            return await _cardDb
                .Card
                .Include(s => s.Suit)
                .Include(_ => _.Value)
                .SingleAsync(_ => _.Id == id);
        }

        public Card GetCard(int id)
        {
            return _cardDb
                .Card
                .Include(s => s.Suit)
                .Include(_ => _.Value)
                .Single(_ => _.Id == id);
        }

        public Card GetCard(SuitEnum suit, ValueEnum value)
        {
            return _cardDb.Card.Single(_ => _.Suit.Name == suit.ToString() && _.Value.Name == value.ToString());
        }

        public bool SeedSuits()
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

            foreach (var suit in suits)
            {
                if (!_cardDb.Suit.Any(_ => _.Name.Equals(suit.Name)))
                {
                    _cardDb.Suit.Add(suit);
                    _cardDb.SaveChanges();
                }
            }
            return true;
        }

        public bool SeedValues()
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
                    Points = 0,
                    Sequence = 20
                },
                new Value()
                {
                    Name = "Nine",
                    Char = "9",
                    Points = 0,
                    Sequence = 30
                },
                new Value()
                {
                    Name = "Ten",
                    Char = "T",
                    Points = 10,
                    Sequence = 40
                },
                new Value()
                {
                    Name = "Joker",
                    Char = "J",
                    Points = 1,
                    Sequence = 50
                },
                new Value()
                {
                    Name = "Jack",
                    Char = "J",
                    Points = 1,
                    Sequence = 60
                },
                new Value()
                {
                    Name = "Queen",
                    Char = "Q",
                    Points = 2,
                    Sequence = 70
                },
                new Value()
                {
                    Name = "King",
                    Char = "K",
                    Points = 3,
                    Sequence = 80
                },
                new Value()
                {
                    Name = "Ace",
                    Char = "A",
                    Points = 4,
                    Sequence = 90
                },
            };

            foreach (var value in values)
            {
                if (!_cardDb.Value.Any(_ => _.Name.Equals(value.Name)))
                {
                    _cardDb.Value.Add(value);
                    _cardDb.SaveChanges();
                }
            }
            return true;
        }

        public async Task<Card> GetCard(int suitId, ValueEnum valueEnum)
        {
            return await _cardDb
                .Card
                .Include(s => s.Suit)
                .Include(_ => _.Value)
                .SingleAsync(_ => _.SuitId == suitId && _.Value.Name == valueEnum.ToString());
        }

        public bool SeedCards()
        {
            foreach (SuitEnum suit in Enum.GetValues(typeof(SuitEnum)))
            {
                if (suit != SuitEnum.None)
                {
                    foreach (ValueEnum value in Enum.GetValues(typeof(ValueEnum)))
                    {
                        if (value != ValueEnum.Joker)
                        {
                            var suitId = _cardDb.Suit.Single(_ => _.Name.Equals(suit.ToString())).Id;
                            var valueId = _cardDb.Value.Single(_ => _.Name.Equals(value.ToString())).Id;
                            if (!_cardDb.Card.Any(_ => _.SuitId == suitId && _.ValueId == valueId))
                            {
                                var card = new Card()
                                {
                                    SuitId = suitId,
                                    ValueId = valueId
                                };
                                _cardDb.Card.Add(card);
                                _cardDb.SaveChanges();
                            }
                        }
                    }
                }
            }
            var noneSuitId = _cardDb.Suit.Single(_ => _.Name.Equals(SuitEnum.None.ToString())).Id;
            var jokerValueId = _cardDb.Value.Single(_ => _.Name.Equals(ValueEnum.Joker.ToString())).Id;
            if (!_cardDb.Card.Any(_ => _.SuitId == noneSuitId && _.ValueId == jokerValueId))
            {
                var joker = new Card()
                {
                    SuitId = noneSuitId,
                    ValueId = jokerValueId
                };

                _cardDb.Card.Add(joker);
                _cardDb.SaveChanges();
            }
            return true;
        }
    }
}
