using System;
using System.Threading.Tasks;
using System.Numerics;
using TipBot.Mongo;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using TipBot.Games;
using TipBot.EmbedMessages;
using Discord;
namespace TipBot.TransferHelper.PendingObjects {
    public enum BlackJackState {
        Start,
        Hit,
        Stand,
        Double
    }
    public class PendingBlackjack {
        public IUser User;
        public string Value;
        public string Symbol;
        public ulong Seed;
        public BlackJackState State;
        public IMessageChannel Channel;
        public IUserMessage Message;
        public Blackjack Instance;

        public PendingBlackjack(IUser user, string v, string s, ulong seed, IMessageChannel chan, BlackJackState state, IUserMessage msg = null, Blackjack instance = null) {
            User = user;
            Value = v;
            Symbol = s;
            Seed = seed;
            Channel = chan;
            Message = msg;
            State = state;
            Instance = instance;
        }
        public PendingBlackjack(IUser user, BlackJackState state, Blackjack instance, IUserMessage msg) {
            Message = msg;
            State = state;
            Instance = instance;
            User = user;
        }

        public async Task HandleState() {
            Console.WriteLine("State handler");
            using (var session = DatabaseConnection.GetClient().StartSession()) {
                try {
                    session.StartTransaction();
                    switch (State) {
                        case BlackJackState.Start:
                            await HandleStart(session);
                            break;
                        case BlackJackState.Hit:
                            await Instance.PlayerExecution(Message, session);
                            break;
                        case BlackJackState.Double:
                            await HandleDoubleUp(session);
                            break;
                        case BlackJackState.Stand:
                            await Instance.DealerExecution(Message, session);
                            break;
                    }
                    await session.CommitTransactionAsync();
                }
                catch (Exception e) {
                    Logger.LogInternal("Blackjack state handler error : " + e.Message);
                    await session.AbortTransactionAsync();
                }
            }
        }

        public async Task HandleStart(IClientSessionHandle session) {
            var previousInstance = await Blackjack.GetBlackJackInstance(User.Id);
            if (previousInstance != null)
                await HandleOngoingGame(previousInstance);
            else {
                var gamblingObject = await GamblingObject.GetGamblingApproval(User.Id, Value, Symbol, 5, 2);
                if (!gamblingObject.Approved) {
                    await User.SendMessageAsync(embed: Embeds.BasicEmbed("Gambling error", "🚫 " + gamblingObject.ErrorMessage, Color.Red));
                    return;
                }
                await CasinoGames.TransferToBankroll(gamblingObject.SenderWallet, gamblingObject.BankRoll, Value, session);
                var blackjackInstance = new Blackjack(Seed.ToString(), gamblingObject.CurrentToken.Symbol, gamblingObject.SenderWallet.ParseValue(Value), User.Id);
                var reactMsg = await blackjackInstance.HandleBlackJackGame(session, Channel, Message);
                if (Message == null)
                    await reactMsg.AddReactionsAsync(new IEmote[4] { new Emoji("✅"), new Emoji("🚫"), new Emoji("⏫"), new Emoji("🔄") });
                GamblingCommands.UpdateBlackjackFocus(User.Id, reactMsg, GameEnum.Blackjack, "", Value, Symbol);
            }
        }

        public async Task HandleDoubleUp(IClientSessionHandle session) {
            if (Instance.PlayerCards.Count > 2)
                return;
            var gamblingObjectDoubleUp = await GamblingObject.GetGamblingApproval(Instance.PlayerId, GamblingCommands.focusBlackjack[Instance.PlayerId].Value, GamblingCommands.focusBlackjack[Instance.PlayerId].Symbol, 1, 1);
            Instance.BetValue = (BigInteger.Parse(Instance.BetValue) * 2).ToString();
            if (!gamblingObjectDoubleUp.Approved) {
                await User.SendMessageAsync(embed: Embeds.BasicEmbed("🚫 Blackjack Error", gamblingObjectDoubleUp.ErrorMessage, Color.Red));
                GamblingCommands.RemoveBlackjackFocus(Instance.PlayerId);
                return;
            }
            await CasinoGames.TransferToBankroll(gamblingObjectDoubleUp.SenderWallet, gamblingObjectDoubleUp.BankRoll, GamblingCommands.focusBlackjack[Instance.PlayerId].Value, session);
            Instance.AddCard("player");
            if (Instance.CheckIfBusts("player")) {
                var embed = await Instance.GetBlackjackLoseEmbed();
                await Message.ModifyAsync(m => m.Embed = embed.Build());
                await Instance.DeleteFromDatabase(session);
                await TipPlayerStats.LogBetToDatabase(Instance.PlayerId, Instance.TokenSymbol, BigInteger.Parse(Instance.BetValue), BigInteger.Zero);
                return;
            }
            await Instance.DealerExecution(Message, session);
        }

        public async Task HandleOngoingGame(Blackjack previousInstance) {
            IUserMessage reactMsg = null;
            if (Convert.ToInt32(((DateTimeOffset)(DateTime.UtcNow)).ToUnixTimeSeconds()) - previousInstance.TimeStamp < 24 * 3600) {
                reactMsg = await Channel.SendMessageAsync(embed: (await previousInstance.GetBlackjackOngoingEmbed()).Build());
                await reactMsg.AddReactionsAsync(new IEmote[4] { new Emoji("✅"), new Emoji("🚫"), new Emoji("⏫"), new Emoji("🔄") });
                if (GamblingCommands.focusBlackjack.ContainsKey(User.Id))
                    GamblingCommands.focusBlackjack[User.Id].UpdateFocus(reactMsg);
                else {
                    var token = await TransferHelper.ServiceData.GetTokenSymbol(previousInstance.TokenSymbol);
                    GamblingCommands.focusBlackjack.Add(User.Id,
                        new GambleReactionFocus(reactMsg, GameEnum.Blackjack, "",
                        TransferHelper.TransferFunctions.FormatUint(previousInstance.BetValue, token.Decimal, true), previousInstance.TokenSymbol));
                }
            }
            else {
                await Channel.SendMessageAsync("You took too long to finish your previous Blackjack game. You have lost your bet");
                await previousInstance.DeleteFromDatabase();
            }
        }
    }
}
