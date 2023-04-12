using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using TipBot.TransferHelper;
using TipBot.EmbedMessages;
using TipBot.TransferHelper.PendingObjects;
namespace TipBot {
    public class WithdrawalReactionFocus : ReactionFocus {
        public string Value;
        public string Symbol;
        public string Fee;
        public WithdrawalReactionFocus(string value, string symbol, string fee, IMessage msg) : base(msg) {
            Fee = fee;
            Value = value;
            Symbol = symbol;
        }

        public void UpdateWithdrawalFocus(string value, string symbol, string fee, IMessage msg) {
            Fee = fee;
            Value = value;
            Symbol = symbol;
            UpdateFocus(msg);
        }

        public static async Task HandleWithdrawalReactions(Cacheable<IUserMessage, ulong> msg, Cacheable<IMessageChannel, ulong> chan, SocketReaction reaction) {
            //public static async Task HandleWithdrawalReactions(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel chan, SocketReaction reaction) {
            try {
                var focusDic = TipCommands.focusWithdrawalDictionary;
                var keyList = new List<ulong>();
                var now = Convert.ToInt32(((DateTimeOffset)(DateTime.UtcNow)).ToUnixTimeSeconds());
                foreach (var keyPair in focusDic) {
                    if (now - keyPair.Value.TimeStamp >= 30)
                        keyList.Add(keyPair.Key);
                }
                foreach (var key in keyList)
                    focusDic.Remove(key);
                if (focusDic.ContainsKey(reaction.UserId)) {
                    var botMsg = focusDic[reaction.UserId].Message as IUserMessage;
                    if (focusDic[reaction.UserId].Message.Id == msg.Id && now - focusDic[reaction.UserId].TimeStamp < 30) {
                        var withdrawal = focusDic[reaction.UserId];
                        var embed = botMsg.Embeds.First();
                        switch (reaction.Emote.Name) {

                            case "✅":
                                await botMsg.ModifyAsync(m => m.Embed = embed.ToEmbedBuilder().WithColor(Color.Green).WithDescription("Your withdrawal has been logged and will soon be executed").Build());
                                lock (PendingQueue.obj) {
                                    PendingQueue.TransactionQueue.Enqueue(new PendingTransaction(Log.TransactionType.Withdrawal,
                                        new PendingWithdrawal(reaction.UserId, focusDic[reaction.UserId].Value, focusDic[reaction.UserId].Symbol, focusDic[reaction.UserId].Fee)));
                                }
                                focusDic.Remove(reaction.UserId);
                                break;
                            case "🚫":
                                focusDic.Remove(reaction.UserId);
                                await msg.Value.ModifyAsync(m => m.Embed = Embeds.BasicEmbed($"Withdrawal query cancelled", "", Color.Red));
                                break;
                        }
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }
    }
}
