using Microsoft.EntityFrameworkCore;
using Shmear.EntityFramework.EntityFrameworkCore.SqlServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shmear.Business.Services
{
    public class PlayerComputerService
    {
        public double Aggressiveness = 50.0d;
        public int PointsToWinRound = 11;
        
        public static async Task<KeyValuePair<CardService.SuitEnum, double>> Wager(DbContextOptions<CardContext> options, int gameId, int playerId)
        {
            var game = await GameService.GetGame(options, gameId);
            var gamePlayer = await GameService.GetGamePlayer(options, gameId, playerId);
            SetAggressiveness(gamePlayer.SeatNumber, game.Team1Points, game.Team2Points);
            return await CalculateBestHandValuePerSuit(options, gameId, playerId);
        }

        private static async Task<KeyValuePair<CardService.SuitEnum, double>> CalculateBestHandValuePerSuit(DbContextOptions<CardContext> options, int gameId, int playerId)
        {
            var suitWorth = await CalculateHandValuePerSuit(options, gameId, playerId);
            // Use first in case there is a tie
            var bestSuit = suitWorth.Keys.First(_ => suitWorth[_] == suitWorth.Values.Max());
            var bestWorth = suitWorth[bestSuit];

            return new KeyValuePair<CardService.SuitEnum, double>(bestSuit, bestWorth);
        }

        private static async Task<Dictionary<CardService.SuitEnum, double>> CalculateHandValuePerSuit(DbContextOptions<CardContext> options, int gameId, int playerId)
        {
            var valueWorthTrump = LoadValueWorthTrumpDictionary();
            var valueWorthOffSuit = LoadValueWorthOffSuitDictionary();
            var handCards = await HandService.GetHand(options, gameId, playerId);
            var dictionary = new Dictionary<CardService.SuitEnum, double>();
            foreach (var suit in (CardService.SuitEnum[])Enum.GetValues(typeof(CardService.SuitEnum)))
            {
                if (suit == CardService.SuitEnum.None)
                    continue;

                double value = 0.0;
                foreach (var handCard in handCards.Where(_ => _.Card.Suit.Name == suit.ToString() || _.Card.Value.Name == CardService.ValueEnum.Joker.ToString()))
                    value += valueWorthTrump[(CardService.ValueEnum)Enum.Parse(typeof(CardService.ValueEnum), handCard.Card.Value.Name)];
                foreach (var handCard in handCards.Where(_ => _.Card.Suit.Name != suit.ToString() && _.Card.Value.Name != CardService.ValueEnum.Joker.ToString()))
                    value += valueWorthOffSuit[(CardService.ValueEnum)Enum.Parse(typeof(CardService.ValueEnum), handCard.Card.Value.Name)];

                dictionary.Add(suit, value);
            }

            return dictionary;
        }

        public static async Task<Card> PlayCard(DbContextOptions<CardContext> options, int gameId, int playerId)
        {
            var board = await BoardService.GetBoardByGameId(options, gameId);
            var handCards = await HandService.GetHand(options, gameId, playerId);
            var tricks = await TrickService.GetTricks(options, gameId);
            var trickCards = await TrickService.GetAllTrickCards(options, gameId);


            // If first trick, determine best card to play as first card
            if (tricks.Count() == 1 && tricks.Single().TrickCard.Count() == 0)
            {
                var suit = (await CalculateBestHandValuePerSuit(options, gameId, playerId)).Key;
                var bestSuitCard = GetBestFirstPlayCard(handCards.Where(_ => _.Card.Suit.Name == suit.ToString()).Select(_ => _.Card));
                return bestSuitCard;
            }

            // If only one card is allowed to be played, play it
            var validCards = new List<Card>();
            foreach (var handCard in handCards)
            {
                if (await GameService.ValidCardPlay(options, gameId, board.Id, playerId, handCard.CardId))
                    validCards.Add(handCard.Card);
            }
            if (validCards.Count() == 1)
                return validCards.Single();

            // if your lead
            var latestTrick = tricks.OrderByDescending(_ => _.CreatedDate).First();
            if (!trickCards.Any(_ => _.TrickId == latestTrick.Id))
            {
                // check to see if jack of trump suit or joker haven't been played
                var jackOrJoker = trickCards.Where(_ => (_.Card.SuitId == board.TrumpSuitId && _.Card.Value.Name == CardService.ValueEnum.Jack.ToString()) || _.Card.Value.Name == CardService.ValueEnum.Joker.ToString());
                if (!jackOrJoker.Any())
                {
                    // check to see if you can take the jack or joker
                    var jackTrumpCard = await CardService.GetCard(options, (int)board.TrumpSuitId, CardService.ValueEnum.Jack);
                    var beatJackOrJoker = handCards.Where(_ => _.Card.Value.Sequence > jackTrumpCard.Value.Sequence);
                    if (beatJackOrJoker.Any())
                        return beatJackOrJoker.OrderByDescending(_ => _.Card.Value.Sequence).First().Card;
                }
                else
                {
                    // Play highest card of non-trump suit that isn't a 10
                    var highestNonTrumpNonTenCard = validCards.Where(_ => _.SuitId != (int)board.TrumpSuitId && _.Value.Name == CardService.ValueEnum.Ten.ToString()).OrderByDescending(_ => _.Value.Sequence).FirstOrDefault();
                    if (highestNonTrumpNonTenCard != null)
                        return highestNonTrumpNonTenCard;
                }
            }
            else
            {
                // if not your lead
                // if your team isn't winning and you can't win, throw lowest card (10 is rated as lowest value option)
                var winningCard = BoardService.DetermineWinningCard(options, gameId, trickCards);

                // if your team is winning
                    

                
            }

            var trumpSuitId = board.TrumpSuitId;
            //var latestTrick = tricks.OrderByDescending(_ => _.CreatedDate);

            return null;
        }

        private static Card GetBestFirstPlayCard(IEnumerable<Card> cards)
        {
            var card = cards.SingleOrDefault(_ => _.Value.Name == CardService.ValueEnum.Ace.ToString());
            if (card != null)
                return card;
            card = cards.SingleOrDefault(_ => _.Value.Name == CardService.ValueEnum.King.ToString());
            if (card != null)
                return card;
            card = cards.SingleOrDefault(_ => _.Value.Name == CardService.ValueEnum.Queen.ToString());
            if (card != null)
                return card;
            card = cards.SingleOrDefault(_ => _.Value.Name == CardService.ValueEnum.Seven.ToString());
            if (card != null)
                return card;
            card = cards.SingleOrDefault(_ => _.Value.Name == CardService.ValueEnum.Eight.ToString());
            if (card != null)
                return card;
            card = cards.SingleOrDefault(_ => _.Value.Name == CardService.ValueEnum.Nine.ToString());
            if (card != null)
                return card;
            card = cards.SingleOrDefault(_ => _.Value.Name == CardService.ValueEnum.Ten.ToString());
            if (card != null)
                return card;
            card = cards.SingleOrDefault(_ => _.Value.Name == CardService.ValueEnum.Jack.ToString());
            if (card != null)
                return card;
            throw new NotImplementedException();
        }

        private static void SetAggressiveness(int playerSeat, int team1Points, int team2Points)
        {
            double difference = team1Points - team2Points;
            if (playerSeat == 1 || playerSeat == 3)
                difference = team2Points - team1Points;

            difference = difference / 11;

            // if number is greater than 0
                // the closer to zero, the more aggressive

            // if the number is less than 0
                // the closer to zero, the less aggressive

        }

        private static Dictionary<CardService.ValueEnum, double> LoadValueWorthTrumpDictionary()
        {
            var valueWorthTrump = new Dictionary<CardService.ValueEnum, double>();
            valueWorthTrump.Add(CardService.ValueEnum.Ace, 2.0);
            valueWorthTrump.Add(CardService.ValueEnum.King, 1.6);
            valueWorthTrump.Add(CardService.ValueEnum.Queen, 1.3);
            valueWorthTrump.Add(CardService.ValueEnum.Jack, 1.0);
            valueWorthTrump.Add(CardService.ValueEnum.Joker, 0.9);
            valueWorthTrump.Add(CardService.ValueEnum.Ten, 0.7);
            valueWorthTrump.Add(CardService.ValueEnum.Nine, 0.4);
            valueWorthTrump.Add(CardService.ValueEnum.Eight, 0.3);
            valueWorthTrump.Add(CardService.ValueEnum.Seven, 1.0);
            return valueWorthTrump;
        }

        private static Dictionary<CardService.ValueEnum, double> LoadValueWorthOffSuitDictionary()
        {
            var valueWorthOffSuit = new Dictionary<CardService.ValueEnum, double>();
            valueWorthOffSuit.Add(CardService.ValueEnum.Ace, 0.2);
            valueWorthOffSuit.Add(CardService.ValueEnum.King, 0.1);
            valueWorthOffSuit.Add(CardService.ValueEnum.Queen, 0.0);
            valueWorthOffSuit.Add(CardService.ValueEnum.Jack, 0.0);
            valueWorthOffSuit.Add(CardService.ValueEnum.Ten, 0.0);
            valueWorthOffSuit.Add(CardService.ValueEnum.Nine, 0.0);
            valueWorthOffSuit.Add(CardService.ValueEnum.Eight, 0.0);
            valueWorthOffSuit.Add(CardService.ValueEnum.Seven, 0.0);
            return valueWorthOffSuit;
        }
    }
}
