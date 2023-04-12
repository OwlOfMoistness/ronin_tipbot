using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using TipBot.EmbedMessages;
namespace TipBot {
    public class ReactionFocus {
        public IMessage Message;
        public int TimeStamp;

        public ReactionFocus(IMessage msg) {
            Message = msg;
            TimeStamp = Convert.ToInt32(((DateTimeOffset)(DateTime.UtcNow)).ToUnixTimeSeconds());
        }

        public void UpdateFocus(IMessage msg) {
            Message = msg;
            TimeStamp = Convert.ToInt32(((DateTimeOffset)(DateTime.UtcNow)).ToUnixTimeSeconds());
        }

        public static async Task HandleHelpReactions(Cacheable<IUserMessage, ulong> msg, Cacheable<IMessageChannel, ulong> chan, SocketReaction reaction) {
        //public static async Task HandleHelpReactions(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel chan, SocketReaction reaction) {
            var focusDic = TipCommands.focusHelpDictionary;
            var keyList = new List<ulong>();
            var now = Convert.ToInt32(((DateTimeOffset)(DateTime.UtcNow)).ToUnixTimeSeconds());
            foreach (var keyPair in focusDic) {
                if (now - keyPair.Value.TimeStamp >= 120)
                    keyList.Add(keyPair.Key);
            }
            foreach (var key in keyList)
                focusDic.Remove(key);
            if (focusDic.ContainsKey(reaction.UserId)) {
                if (focusDic[reaction.UserId].Message.Id == msg.Value.Id && now - focusDic[reaction.UserId].TimeStamp < 120) {
                    switch (reaction.Emote.Name) {
                        case "👋":
                            await msg.Value.ModifyAsync(m => m.Embed = HelpMessages.FillIntroMessage().Build());
                            break;
                        case "💰":
                            await msg.Value.ModifyAsync(m => m.Embed = HelpMessages.FillWalletMessage().Build());
                            break;
                        case "🎲":
                            await msg.Value.ModifyAsync(m => m.Embed = HelpMessages.FillGamblingMessage().Build());
                            break;
                    }
                    await msg.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                }
            }
        }
    }

}
