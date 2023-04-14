using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using TipBot.TransferHelper.PendingObjects;
using TipBot.Games;
using TipBot.TransferHelper;
using TipBot.EmbedMessages;

namespace TipBot {
    [Group("gamble")]
    public class GamblingCommands : ModuleBase {
        public static Dictionary<ulong, GambleReactionFocus> focusBlackjack;
        public static Dictionary<ulong, GambleReactionFocus> focusBet;
        public static readonly Dictionary<string, BlackJackState> emoteMap;

        static GamblingCommands() {
            focusBlackjack = new Dictionary<ulong, GambleReactionFocus>();
            focusBet = new Dictionary<ulong, GambleReactionFocus>();
            emoteMap = new Dictionary<string, BlackJackState>() {
                {"✅", BlackJackState.Hit },
                {"🚫", BlackJackState.Stand },
                {"⏫", BlackJackState.Double }
            };
        }

        //[Command("stats")]
        //[Alias("stat")]
        //public async Task GetPlayerStats(string symbol) {
        //    var stats = await TipPlayerStats.GetStats(Context.Message.Author.Id);
        //    var token = await ServiceData.GetTokenSymbol(symbol);
        //    if (stats == null || token == null)
        //        return;
        //    await ReplyAsync(embed: Embeds.GetPlayerStats(stats, token, Context.Message.Author.Username));
        //}

        //[Command("etheroll", RunMode = RunMode.Async)]
        //[Alias("er", "roll")]
        //public async Task Etheroll(string amount, string symbol, int rollUnder) {
        //    if (Context.Guild != null && !await GuildParametres.CanPlay(Context.Guild.Id, Context.Channel.Id))
        //        return;
        //    if (rollUnder < 2 || rollUnder > 99) {
        //        await ReplyAsync("🚫 Roll_under value must be between [2;99]");
        //        return;
        //    }
        //    lock (PendingQueue.obj) {
        //        PendingQueue.TransactionQueue.Enqueue(new PendingTransaction(Log.TransactionType.Game,
        //            new PendingGame(Context.Message.Author, amount, symbol, rollUnder.ToString(), 100, rollUnder - 1, GameEnum.Etheroll, Context.Message.Id, Context.Channel)));
        //    }
        //}

        //[Command("coinflip", RunMode = RunMode.Async)]
        //[Alias("coin", "flip", "cf")]
        //public async Task Coinflip(string amount, string symbol, string outcome) {
        //    if (Context.Guild != null && !await GuildParametres.CanPlay(Context.Guild.Id, Context.Channel.Id))
        //        return;
        //    if (outcome.ToLower() != "heads" && outcome.ToLower() != "tails") {
        //        await ReplyAsync("🚫 Possible outcomes are `heads` or `tails`");
        //        return;
        //    }
        //    lock (PendingQueue.obj) {
        //        PendingQueue.TransactionQueue.Enqueue(new PendingTransaction(Log.TransactionType.Game,
        //            new PendingGame(Context.Message.Author, amount, symbol, outcome, 2, 1, GameEnum.Coinflip, Context.Message.Id, Context.Channel)));
        //    }
        //}


        //[Command("blackjack", RunMode = RunMode.Async)]
        //[Alias("bj", "21")]
        //public async Task BlackjackGame(string amount, string symbol) {
        //    if (Context.Guild != null && !await GuildParametres.CanPlay(Context.Guild.Id, Context.Channel.Id))
        //        return;
        //    IUserMessage reactMsg = null;
        //    var previousInstance = await Blackjack.GetBlackJackInstance(Context.Message.Author.Id);
        //    if (previousInstance != null) {
        //        if (Convert.ToInt32(((DateTimeOffset)(DateTime.UtcNow)).ToUnixTimeSeconds()) - previousInstance.TimeStamp < 24 * 3600) {
        //            reactMsg = await ReplyAsync(embed: (await previousInstance.GetBlackjackOngoingEmbed()).Build());
        //            await reactMsg.AddReactionsAsync(new IEmote[4] { new Emoji("✅"), new Emoji("🚫"), new Emoji("⏫"), new Emoji("🔄") });
        //            if (focusBlackjack.ContainsKey(Context.Message.Author.Id))
        //                focusBlackjack[Context.Message.Author.Id].UpdateFocus(reactMsg);
        //            else {
        //                var token = await TransferHelper.ServiceData.GetTokenSymbol(previousInstance.TokenSymbol);
        //                focusBlackjack.Add(Context.Message.Author.Id,
        //                    new GambleReactionFocus(reactMsg, GameEnum.Blackjack, "",
        //                    TransferHelper.TransferFunctions.FormatUint(previousInstance.BetValue, token.Decimal, true), previousInstance.TokenSymbol));
        //            }
        //        }
        //        else {
        //            await ReplyAsync("You took too long to finish your previous Blackjack game. You have lost your bet");
        //            await previousInstance.DeleteFromDatabase();
        //        }

        //    }
        //    else {
        //        lock (PendingQueue.obj) {
        //            PendingQueue.TransactionQueue.Enqueue(new PendingTransaction(Log.TransactionType.Blackjack,
        //                new PendingBlackjack(Context.Message.Author, amount, symbol, Context.Message.Id, Context.Channel, BlackJackState.Start)));
        //        }
        //    }
        //}

        public static void UpdateGambleFocus(ulong id, IUserMessage reactMsg, GameEnum game, string betCondition, string amount, string symbol) {
            if (focusBet.ContainsKey(id))
                focusBet[id].UpdateGameFocus(reactMsg, game, betCondition, amount, symbol);
            else
                focusBet.Add(id, new GambleReactionFocus(reactMsg, game, betCondition, amount, symbol));
        }

        public static void UpdateBlackjackFocus(ulong id, IUserMessage reactMsg, GameEnum game, string betCondition, string amount, string symbol) {
            if (focusBlackjack.ContainsKey(id))
                focusBlackjack[id].UpdateGameFocus(reactMsg, game, betCondition, amount, symbol);
            else
                focusBlackjack.Add(id, new GambleReactionFocus(reactMsg, game, betCondition, amount, symbol));
        }

        public static void RemoveBlackjackFocus(ulong id) {
            if (focusBlackjack.ContainsKey(id))
                focusBlackjack.Remove(id);
        }

        public static async Task HandleRebetCoinReactions(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction) {
            if (reaction.User.Value.IsBot)
                return;
            var focusDic = focusBet;
            var keyList = new List<ulong>();
            var now = Convert.ToInt32(((DateTimeOffset)(DateTime.UtcNow)).ToUnixTimeSeconds());
            foreach (var keyPair in focusDic) {
                if (now - keyPair.Value.TimeStamp >= 600)
                    keyList.Add(keyPair.Key);
            }
            foreach (var key in keyList)
                focusDic.Remove(key);
            if (focusDic.ContainsKey(reaction.UserId)) {
                if (focusDic[reaction.UserId].Message.Id == msg.Value.Id && now - focusDic[reaction.UserId].TimeStamp < 600) {
                    focusDic[reaction.UserId].UpdateFocus(msg.Value);
                    if (reaction.Emote.Name == "🔄") {
                        if (!focusDic[reaction.UserId].InGame) {
                            if (focusDic[reaction.UserId].Game == GameEnum.Coinflip)
                                lock (PendingQueue.obj) {
                                    PendingQueue.TransactionQueue.Enqueue(new PendingTransaction(Log.TransactionType.Game,
                                        new PendingGame(reaction.User.Value,
                                        focusDic[reaction.UserId].Value,
                                        focusDic[reaction.UserId].Symbol,
                                        focusDic[reaction.UserId].Condition, 2, 1, GameEnum.Coinflip, msg.Value.Id, channel, msg.Value)));
                                }
                            else if (focusDic[reaction.UserId].Game == GameEnum.Etheroll)
                                lock (PendingQueue.obj) {
                                    PendingQueue.TransactionQueue.Enqueue(new PendingTransaction(Log.TransactionType.Game,
                                        new PendingGame(reaction.User.Value,
                                        focusDic[reaction.UserId].Value,
                                        focusDic[reaction.UserId].Symbol,
                                        focusDic[reaction.UserId].Condition, 100,
                                        Convert.ToInt32(focusDic[reaction.UserId].Condition) - 1,
                                        GameEnum.Etheroll, msg.Value.Id, channel, msg.Value)));
                                }
                        }
                        await msg.Value.RemoveReactionAsync(new Emoji("🔄"), reaction.User.Value);
                    }
                }
            }
        }

        public static async Task HandleBlackjackReactions(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel, SocketReaction reaction) {
            if (reaction.User.Value.IsBot)
                return;
            var focusDic = focusBlackjack;
            var keyList = new List<ulong>();
            var now = Convert.ToInt32(((DateTimeOffset)(DateTime.UtcNow)).ToUnixTimeSeconds());
            foreach (var keyPair in focusDic) {
                if (now - keyPair.Value.TimeStamp >= 600)
                    keyList.Add(keyPair.Key);
            }
            foreach (var key in keyList)
                focusDic.Remove(key);
            if (focusDic.ContainsKey(reaction.UserId)) {
                var instance = await Blackjack.GetBlackJackInstance(reaction.UserId);
                if (focusDic[reaction.UserId].Message.Id == msg.Value.Id && now - focusDic[reaction.UserId].TimeStamp < 600) {
                    focusDic[reaction.UserId].UpdateFocus(msg.Value);
                    if (instance != null && (reaction.Emote.Name == "✅" || reaction.Emote.Name == "🚫" || reaction.Emote.Name == "⏫")) {
                        lock (PendingQueue.obj) {
                            PendingQueue.TransactionQueue.Enqueue(new PendingTransaction(Log.TransactionType.Blackjack,
                                new PendingBlackjack(reaction.User.Value, emoteMap[reaction.Emote.Name], instance, msg.Value)));
                        }
                    }
                    else if (instance == null && reaction.Emote.Name == "🔄") {
                        lock (PendingQueue.obj) {
                            PendingQueue.TransactionQueue.Enqueue(new PendingTransaction(Log.TransactionType.Blackjack,
                                new PendingBlackjack(reaction.User.Value, focusDic[reaction.UserId].Value, focusDic[reaction.UserId].Symbol, msg.Value.Id, channel, BlackJackState.Start, msg.Value)));
                        }
                    }
                    await msg.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                }
            }
        }
    }
}
