using System;
using Discord;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using TipBot.Mongo;
using TipBot.TransferHelper;
namespace TipBot.Games {
    public class Blackjack {
        public ObjectId id;
        public ulong PlayerId;
        public string CurrentHash;
        public string TokenSymbol;
        public string BetValue;
        public int TimeStamp;
        public List<Card> DealerCards;
        public List<Card> PlayerCards;


        public Blackjack(string msgId, string symbol, string bet, ulong userId) {
            id = ObjectId.GenerateNewId();
            PlayerId = userId;
            CurrentHash = CasinoGames.Keccak256(msgId);
            TokenSymbol = symbol;
            BetValue = bet;
            DealerCards = new List<Card>();
            PlayerCards = new List<Card>();
            TimeStamp = Convert.ToInt32(((DateTimeOffset)(DateTime.UtcNow)).ToUnixTimeSeconds());
        }

        private BigInteger GetNewHash() {
            CurrentHash = CasinoGames.Keccak256(CurrentHash);
            return BigInteger.Parse("0" + CurrentHash, System.Globalization.NumberStyles.AllowHexSpecifier);
        }

        private Card GetNewCard() {
            var newHash = GetNewHash();
            int value = Convert.ToInt32((newHash % 13).ToString()) + 1;
            int suit = Convert.ToInt32((newHash % 4).ToString());
            return new Card((Suits)suit, (CardValue)value);
        }

        public void AddCard(string entity) {
            if (entity == "player")
                PlayerCards.Add(GetNewCard());
            else
                DealerCards.Add(GetNewCard());
        }

        public void InitGame() {
            AddCard("player");
            AddCard("dealer;");
            AddCard("player");
            AddCard("dealer;");
        }

        public int GetCardsValue(string entity, bool max, bool reveal = false) {
            var set = entity == "player" ? PlayerCards : DealerCards;
            var total = 0;
            var aceCount = 0;
            var counter = 0;
            foreach (var cards in set) {
                if (entity == "dealer" && !reveal && counter == 1)
                    break;
                if ((int)cards.Value >= (int)CardValue.Ten)
                    total += 10;
                else if (cards.Value == CardValue.A)
                    aceCount++;
                else
                    total += (int)cards.Value;

                counter++;
            }
            if (max && aceCount > 0) {
                if (total + 10 + aceCount <= 21)
                    total += 10 + aceCount;
                else
                    total += aceCount;
            }
            else
                total += aceCount;
            return total;
        }

        public bool CheckIfBusts(string entity) {
            return GetCardsValue(entity, false, true) > 21 || GetCardsValue(entity, true, true) > 21;
        }

        public bool CheckIfBlackjack(string entity) {
            if (GetCardsValue(entity, true, true) == 21)
                return true;
            return false;
        }

        public bool DealerMustHit() {
            return GetCardsValue("dealer", false, true) < 17 && GetCardsValue("dealer", true, true) < 17;
        }

        public async Task SaveToDatabase(IClientSessionHandle session) {
            var blackjackInstance = DatabaseConnection.GetDb().GetCollection<Blackjack>("BlackjackInstance");
            var data = (await blackjackInstance.FindAsync(w => w.id == id)).FirstOrDefault();
            if (data != null) {
                var update = Builders<Blackjack>.Update.Set(b => b.BetValue, BetValue)
                    .Set(b => b.CurrentHash, CurrentHash).Set(b => b.DealerCards, DealerCards)
                    .Set(b => b.PlayerCards, PlayerCards).Set(b => b.TimeStamp, TimeStamp);
                await blackjackInstance.UpdateOneAsync(session, w => w.id == id, update);
            }
            else
                await blackjackInstance.InsertOneAsync(session, this);
        }

        public async Task DeleteFromDatabase(IClientSessionHandle session) {
            var blackjackWallet = DatabaseConnection.GetDb().GetCollection<Blackjack>("BlackjackInstance");
            await blackjackWallet.DeleteOneAsync(session, i => i.id == id);
        }

        public async Task DeleteFromDatabase() {
            var blackjackWallet = DatabaseConnection.GetDb().GetCollection<Blackjack>("BlackjackInstance");
            await blackjackWallet.DeleteOneAsync(i => i.id == id);
        }

        public async Task TransferWin(IClientSessionHandle session, bool blackjack = false) {
            var user = await TipUser.GetUser(PlayerId);
            var wallet = await TipWallet.GetWallet(user.walletList[TokenSymbol]);
            var bankRollUser = await TipUser.GetUser(712976781438615572);
            var bankRoll = await TipWallet.GetWallet(bankRollUser.walletList[TokenSymbol]);
            if (blackjack) {
                var blackJackAmount = BigInteger.Parse(BetValue) * 5 / 2;
                wallet.Add(blackJackAmount);
                bankRoll.Sub(blackJackAmount);
                await TipPlayerStats.LogBetToDatabase(PlayerId, TokenSymbol, BigInteger.Parse(BetValue), BigInteger.Parse(BetValue) * 5 / 2);
            }
            else {
                wallet.Add(BigInteger.Parse(BetValue) * 2);
                bankRoll.Sub(BigInteger.Parse(BetValue) * 2);
                await TipPlayerStats.LogBetToDatabase(PlayerId, TokenSymbol, BigInteger.Parse(BetValue), BigInteger.Parse(BetValue) * 2);
            }
            await wallet.SaveToDatabase(session);
            await bankRoll.SaveToDatabase(session);
        }

        public async Task ReturnBet(IClientSessionHandle session) {
            var user = await TipUser.GetUser(PlayerId);
            var wallet = await TipWallet.GetWallet(user.walletList[TokenSymbol]);
            var bankRollUser = await TipUser.GetUser(712976781438615572);
            var bankRoll = await TipWallet.GetWallet(bankRollUser.walletList[TokenSymbol]);
            wallet.Add(BigInteger.Parse(BetValue));
            bankRoll.Sub(BigInteger.Parse(BetValue));
            await wallet.SaveToDatabase(session);
            await bankRoll.SaveToDatabase(session);
            await TipPlayerStats.LogBetToDatabase(PlayerId, TokenSymbol, BigInteger.Parse(BetValue), BigInteger.Parse(BetValue));
        }

        public static async Task<Blackjack> GetBlackJackInstance(ulong _id) {
            var blackjackWallet = DatabaseConnection.GetDb().GetCollection<Blackjack>("BlackjackInstance");
            return (await blackjackWallet.FindAsync(w => w.PlayerId == _id)).FirstOrDefault();
        }

        public string GetHand(string entity, bool reveal = false) {
            var set = entity == "player" ? PlayerCards : DealerCards;
            var hand = "**";
            var counter = 0;
            foreach (var card in set) {
                if (entity == "dealer" && !reveal && counter == 1)
                    hand += "| ? |";
                else {
                    hand += "|";
                    if (card.Value == CardValue.A || (int)card.Value > (int)CardValue.Ten)
                        hand += card.Value.ToString();
                    else
                        hand += ((int)card.Value).ToString();
                    switch (card.Suit) {
                        case Suits.Clubs:
                            hand += "♣️";
                            break;
                        case Suits.Diamonds:
                            hand += "♦️";
                            break;
                        case Suits.Hearts:
                            hand += "♥️";
                            break;
                        case Suits.Spades:
                            hand += "♠️";
                            break;
                    }
                    hand += "|   ";
                    counter++;
                }
            }
            hand = hand.Trim();
            hand += "**";
            return hand;
        }

        public async Task PlayerExecution(IUserMessage msg, IClientSessionHandle session) {
            AddCard("player");
            if (CheckIfBusts("player")) {
                var embed = await GetBlackjackLoseEmbed();
                await msg.ModifyAsync(m => m.Embed = embed.Build());
                await DeleteFromDatabase(session);
                await TipPlayerStats.LogBetToDatabase(PlayerId, TokenSymbol, BigInteger.Parse(BetValue), BigInteger.Zero);
            }
            else if (GetCardsValue("player", true) == 21)
                await DealerExecution(msg, session);
            else {
                var embed = await GetBlackjackOngoingEmbed();
                await msg.ModifyAsync(m => m.Embed = embed.Build());
                await SaveToDatabase(session);
            }
        }

        public async Task DealerExecution(IUserMessage msg, IClientSessionHandle session) {
            while (DealerMustHit())
                AddCard("dealer");
            EmbedBuilder embed = null;
            if (CheckIfBusts("dealer") || GetCardsValue("dealer", true, true) < GetCardsValue("player", true)) {
                embed = await GetBlackjackWinEmbed();
                await TransferWin(session);
            }
            else if (GetCardsValue("dealer", true, true) > GetCardsValue("player", true)) {
                embed = await GetBlackjackLoseEmbed();
                await TipPlayerStats.LogBetToDatabase(PlayerId, TokenSymbol, BigInteger.Parse(BetValue), BigInteger.Zero);
            }
            else {
                await ReturnBet(session);
                embed = await GetBlackjackEvenEmbed();
            }
            await msg.ModifyAsync(m => m.Embed = embed.Build());
            await DeleteFromDatabase(session);
        }

        public async Task<IUserMessage> HandleBlackJackGame(IClientSessionHandle session, IMessageChannel channel, IUserMessage reactMsg = null) {
            InitGame();
            Embed emb = null;
            if (GetCardsValue("player", true) == 21) {
                if (GetCardsValue("dealer", true, true) != 21) {
                    await TransferWin(session, true);
                    emb = (await GetBlackjackW21Embed()).Build();
                }
                else {
                    await ReturnBet(session);
                    emb = (await GetBlackjackEvenEmbed()).Build();
                }
            }
            else if (CheckIfBlackjack("dealer")) {
                emb = (await GetBlackjackLoseEmbed()).Build();
                await TipPlayerStats.LogBetToDatabase(PlayerId, TokenSymbol, BigInteger.Parse(BetValue), BigInteger.Zero);
            }
            else {
                emb = (await GetBlackjackOngoingEmbed()).Build();
                await SaveToDatabase(session);
            }
            if (reactMsg == null)
                reactMsg = await channel.SendMessageAsync(embed: emb);
            else
                await reactMsg.ModifyAsync(m => m.Embed = emb);
            return reactMsg;
        }

        public async Task<EmbedBuilder> GetBlackjackWinEmbed() {
            var token = await ServiceData.GetTokenSymbol(TokenSymbol);
            var embed = new EmbedBuilder();
            var winAmount = BigInteger.Parse(BetValue) * 2;
            embed.WithColor(Color.Green);
            embed.WithTitle($"Blackjack ♣️♥️♠️♦️ You won!");
            embed.WithDescription($"<@!{PlayerId}>, game over");
            embed.AddField("Dealer's hand", $"{GetHand("dealer", true)} **total** = {GetCardsValue("dealer", true, true)}");
            var cardValue = GetCardsValue("player", true);
            embed.AddField("Player's hand", $"{GetHand("player")} **total** = {cardValue} ");
            embed.AddField("Won amount", $"{TransferFunctions.FormatUint(winAmount, token.Decimal)} {TokenSymbol}");
            embed.WithFooter("React ✅ to hit, 🚫 to stand, ⏫ to double, 🔄 to rebet");
            return embed;
        }

        public async Task<EmbedBuilder> GetBlackjackW21Embed() {
            var token = await ServiceData.GetTokenSymbol(TokenSymbol);
            var embed = new EmbedBuilder();
            var winAmount = BigInteger.Parse(BetValue) * 5 / 2;
            embed.WithColor(Color.Gold);
            embed.WithTitle($"Blackjack ♣️♥️♠️♦️ You scored a blackjack!");
            embed.WithDescription($"<@!{PlayerId}>, game over");
            embed.AddField("Dealer's hand", $"{GetHand("dealer", true)} **total** = {GetCardsValue("dealer", true, true)}");
            var cardValue = GetCardsValue("player", true);
            embed.AddField("Player's hand", $"{GetHand("player")} **total** = {cardValue} ");
            embed.AddField("Won amount", $"{TransferFunctions.FormatUint(winAmount, token.Decimal)} {TokenSymbol}");
            embed.WithFooter("React ✅ to hit, 🚫 to stand, ⏫ to double, 🔄 to rebet");
            return embed;
        }

        public async Task<EmbedBuilder> GetBlackjackEvenEmbed() {
            var token = await ServiceData.GetTokenSymbol(TokenSymbol);
            var embed = new EmbedBuilder();
            embed.WithColor(Color.Orange);
            embed.WithTitle($"Blackjack ♣️♥️♠️♦️ Even game, you get your bet back!");
            embed.WithDescription($"<@!{PlayerId}>, game over");
            embed.AddField("Dealer's hand", $"{GetHand("dealer", true)} **total** = {GetCardsValue("dealer", true, true)}");
            var cardValue = GetCardsValue("player", true);
            embed.AddField("Player's hand", $"{GetHand("player")} **total** = {cardValue} ");
            embed.WithFooter("React ✅ to hit, 🚫 to stand, ⏫ to double, 🔄 to rebet");
            return embed;
        }

        public async Task<EmbedBuilder> GetBlackjackLoseEmbed() {
            var token = await ServiceData.GetTokenSymbol(TokenSymbol);
            var embed = new EmbedBuilder();
            embed.WithColor(Color.Red);
            embed.WithTitle($"Blackjack ♣️♥️♠️♦️ You lost!");
            embed.WithDescription($"<@!{PlayerId}>, game over");
            embed.AddField("Dealer's hand", $"{GetHand("dealer", true)} **total** = {GetCardsValue("dealer", true, true)}");
            var cardValue = GetCardsValue("player", true);
            embed.AddField("Player's hand", $"{GetHand("player")} **total** = {cardValue} ");
            embed.AddField("Lost amount", $"{TransferFunctions.FormatUint(BetValue, token.Decimal)} {TokenSymbol}");
            embed.WithFooter("React ✅ to hit, 🚫 to stand, ⏫ to double, 🔄 to rebet");
            return embed;
        }

        public async Task<EmbedBuilder> GetBlackjackOngoingEmbed() {
            var token = await ServiceData.GetTokenSymbol(TokenSymbol);
            var embed = new EmbedBuilder();
            embed.WithColor(Color.Blue);
            embed.WithTitle($"Blackjack ♣️♥️♠️♦️");
            embed.WithDescription($"<@!{PlayerId}>, finish this game to  play another. After 24 hours, you will automatically lose this game\nBet amount = {TransferFunctions.FormatUint(BetValue, token.Decimal)} {TokenSymbol}");
            embed.AddField("Dealer's hand", $"{GetHand("dealer")}");
            var cardValue = GetCardsValue("player", false);
            if (GetCardsValue("player", true) != cardValue)
                embed.AddField("Player's hand", $"{GetHand("player")} **total** = {cardValue} / {GetCardsValue("player", true)}");
            else
                embed.AddField("Player's hand", $"{GetHand("player")} **total** = {cardValue}");
            embed.WithFooter("React ✅ to hit, 🚫 to stand, ⏫ to double");
            return embed;
        }
    }
}
