using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shmear2.Business.Database;
using Shmear2.Business.Models;
using Shmear2.Business.Database.Models;

namespace Shmear2.Business.Services
{
    public class PlayerComputerService : IPlayerComputerService
    {
        public readonly CardDbContext _cardDb;
        public readonly IGameService _gameService;
        public readonly IBoardService _boardService;
        public readonly IHandService _handService;
        public readonly ITrickService _trickService;
        public readonly ICardService _cardService;
        public PlayerComputerService(
            CardDbContext cardDb,
            IGameService gameService,
            IBoardService boardService,
            IHandService handService,
            ITrickService trickService,
            ICardService cardService)
        {
            _cardDb = cardDb;
            _gameService = gameService;
            _boardService = boardService;
            _handService = handService;
            _trickService = trickService;
            _cardService = cardService;
        }

        public async Task<KeyValuePair<SuitEnum, double>> Wager(
            int gameId,
            int playerId)
        {
            var game = await _gameService.GetGame(gameId);
            var gamePlayer = await _gameService.GetGamePlayer(gameId, playerId);
            SetAggressiveness(gamePlayer.SeatNumber, game.Team1Points, game.Team2Points);
            return await CalculateBestHandValuePerSuit(gameId, playerId);
        }

        private async Task<KeyValuePair<SuitEnum, double>> CalculateBestHandValuePerSuit(
            int gameId,
            int playerId)
        {
            var suitWorth = await CalculateHandValuePerSuit(gameId, playerId);
            // Use first in case there is a tie
            var bestSuit = suitWorth.Keys.First(_ => suitWorth[_] == suitWorth.Values.Max());
            var bestWorth = suitWorth[bestSuit];

            return new KeyValuePair<SuitEnum, double>(bestSuit, bestWorth);
        }

        private async Task<Dictionary<SuitEnum, double>> CalculateHandValuePerSuit(
            int gameId,
            int playerId)
        {
            var valueWorthTrump = LoadValueWorthTrumpDictionary();
            var valueWorthOffSuit = LoadValueWorthOffSuitDictionary();
            var handCards = await _handService.GetHand(gameId, playerId);
            var dictionary = new Dictionary<SuitEnum, double>();
            foreach (var suit in (SuitEnum[])Enum.GetValues(typeof(SuitEnum)))
            {
                if (suit == SuitEnum.None)
                    continue;

                double value = 0.0;
                foreach (var handCard in handCards.Where(_ => _.Card.Suit.Name == suit.ToString() || _.Card.Value.Name == ValueEnum.Joker.ToString()))
                    value += valueWorthTrump[(ValueEnum)Enum.Parse(typeof(ValueEnum), handCard.Card.Value.Name)];
                foreach (var handCard in handCards.Where(_ => _.Card.Suit.Name != suit.ToString() && _.Card.Value.Name != ValueEnum.Joker.ToString()))
                    value += valueWorthOffSuit[(ValueEnum)Enum.Parse(typeof(ValueEnum), handCard.Card.Value.Name)];

                dictionary.Add(suit, value);
            }

            return dictionary;
        }

        public async Task<Card> PlayCard(int gameId, int playerId)
        {
            var board = await _boardService.GetBoardByGameId(gameId);
            var handCards = await _handService.GetHand(gameId, playerId);
            var tricks = await _trickService.GetTricks(gameId);
            var trickCards = await _trickService.GetAllTrickCards(gameId);


            // If first trick, determine best card to play as first card
            if (tricks.Count() == 1 && tricks.Single().TrickCard.Count == 0)
            {
                var suit = (await CalculateBestHandValuePerSuit(gameId, playerId)).Key;
                var bestSuitCard = GetBestFirstPlayCard(handCards.Where(_ => _.Card.Suit.Name == suit.ToString()).Select(_ => _.Card));
                return bestSuitCard;
            }

            // If only one card is allowed to be played, play it
            var validCards = new List<Card>();
            foreach (var handCard in handCards)
            {
                if (await _gameService.ValidCardPlay(gameId, board.Id, playerId, handCard.CardId))
                    validCards.Add(handCard.Card);
            }
            if (validCards.Count == 1)
                return validCards.Single();

            // if your lead
            var latestTrick = tricks.OrderByDescending(_ => _.CreatedDate).First();
            if (!trickCards.Any(_ => _.TrickId == latestTrick.Id))
            {
                // check to see if jack of trump suit or joker haven't been played
                var jackOrJoker = trickCards.Where(_ => (_.Card.SuitId == board.TrumpSuitId && _.Card.Value.Name == ValueEnum.Jack.ToString()) || _.Card.Value.Name == ValueEnum.Joker.ToString());
                if (!jackOrJoker.Any())
                {
                    // check to see if you can take the jack or joker
                    var jackTrumpCard = await _cardService.GetCard((int)board.TrumpSuitId, ValueEnum.Jack);
                    var beatJackOrJoker = handCards.Where(_ => _.Card.Value.Sequence > jackTrumpCard.Value.Sequence);
                    if (beatJackOrJoker.Any())
                        return beatJackOrJoker.OrderByDescending(_ => _.Card.Value.Sequence).First().Card;
                }
                else
                {
                    // Play highest card of non-trump suit that isn't a 10
                    var highestNonTrumpNonTenCard = validCards.Where(_ => _.SuitId != (int)board.TrumpSuitId && _.Value.Name == ValueEnum.Ten.ToString()).OrderByDescending(_ => _.Value.Sequence).FirstOrDefault();
                    if (highestNonTrumpNonTenCard != null)
                        return highestNonTrumpNonTenCard;
                }
            }
            else
            {
                // if not your lead
                // if your team isn't winning and you can't win, throw lowest card (10 is rated as lowest value option)
                var winningCard = _boardService.DetermineWinningCard(gameId, trickCards);

                // if your team is winning



            }

            var trumpSuitId = board.TrumpSuitId;
            //var latestTrick = tricks.OrderByDescending(_ => _.CreatedDate);

            return null;
        }

        private Card GetBestFirstPlayCard(IEnumerable<Card> cards)
        {
            var card = cards.SingleOrDefault(_ => _.Value.Name == ValueEnum.Ace.ToString());
            if (card != null)
                return card;
            card = cards.SingleOrDefault(_ => _.Value.Name == ValueEnum.King.ToString());
            if (card != null)
                return card;
            card = cards.SingleOrDefault(_ => _.Value.Name == ValueEnum.Queen.ToString());
            if (card != null)
                return card;
            card = cards.SingleOrDefault(_ => _.Value.Name == ValueEnum.Seven.ToString());
            if (card != null)
                return card;
            card = cards.SingleOrDefault(_ => _.Value.Name == ValueEnum.Eight.ToString());
            if (card != null)
                return card;
            card = cards.SingleOrDefault(_ => _.Value.Name == ValueEnum.Nine.ToString());
            if (card != null)
                return card;
            card = cards.SingleOrDefault(_ => _.Value.Name == ValueEnum.Ten.ToString());
            if (card != null)
                return card;
            card = cards.SingleOrDefault(_ => _.Value.Name == ValueEnum.Jack.ToString());
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

        private static Dictionary<ValueEnum, double> LoadValueWorthTrumpDictionary()
        {
            var valueWorthTrump = new Dictionary<ValueEnum, double>();
            valueWorthTrump.Add(ValueEnum.Ace, 2.0);
            valueWorthTrump.Add(ValueEnum.King, 1.6);
            valueWorthTrump.Add(ValueEnum.Queen, 1.3);
            valueWorthTrump.Add(ValueEnum.Jack, 1.0);
            valueWorthTrump.Add(ValueEnum.Joker, 0.9);
            valueWorthTrump.Add(ValueEnum.Ten, 0.7);
            valueWorthTrump.Add(ValueEnum.Nine, 0.4);
            valueWorthTrump.Add(ValueEnum.Eight, 0.3);
            valueWorthTrump.Add(ValueEnum.Seven, 1.0);
            return valueWorthTrump;
        }

        private static Dictionary<ValueEnum, double> LoadValueWorthOffSuitDictionary()
        {
            var valueWorthOffSuit = new Dictionary<ValueEnum, double>();
            valueWorthOffSuit.Add(ValueEnum.Ace, 0.2);
            valueWorthOffSuit.Add(ValueEnum.King, 0.1);
            valueWorthOffSuit.Add(ValueEnum.Queen, 0.0);
            valueWorthOffSuit.Add(ValueEnum.Jack, 0.0);
            valueWorthOffSuit.Add(ValueEnum.Ten, 0.0);
            valueWorthOffSuit.Add(ValueEnum.Nine, 0.0);
            valueWorthOffSuit.Add(ValueEnum.Eight, 0.0);
            valueWorthOffSuit.Add(ValueEnum.Seven, 0.0);
            return valueWorthOffSuit;
        }
    }
}
